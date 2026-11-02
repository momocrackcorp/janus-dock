using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>
/// On-demand, paged cascading folder browser for every folder/drive on the Dock.
/// No recursive traversal or unbounded ToolStrip menus. Slow folders are
/// enumerated off the UI thread, and closing a menu cancels pending updates.
/// </summary>
internal static class DockFolderTreeMenu
{
    private const int FolderPageSize = 18;
    private const int FilePageSize = 10;

    private sealed record Entry(string Path, string Name, bool Folder, bool Link = false);
    private sealed record Page(
        List<Entry> Folders, List<Entry> Files, bool More, string? Error);

    private sealed class MenuSession
    {
        readonly CancellationTokenSource cancellation = new();
        public CancellationToken Token => cancellation.Token;
        public bool Closed => cancellation.IsCancellationRequested;
        public void Close()
        {
            if (!Closed) cancellation.Cancel();
        }
    }

    public static bool CanBrowse(string path)
    {
        if (path is "shell:MyComputerFolder" or "janus:documents" or "janus:downloads")
            return true;
        if (Directory.Exists(path)) return true;
        return TryShortcutFolder(path) != null;
    }

    private static string Resolve(string path) => path switch
    {
        "janus:documents" => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "janus:downloads" => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
        _ => TryShortcutFolder(path) ?? path
    };

    // WScript.Shell is part of Windows: it resolves .lnk targets without
    // executing any shortcuts, installing packages or calling PowerShell.
    private static string? TryShortcutFolder(string path)
    {
        if (!path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)
            || !File.Exists(path)) return null;
        object? shell = null, shortcut = null;
        try
        {
            var progId = Type.GetTypeFromProgID("WScript.Shell");
            if (progId == null) return null;
            shell = Activator.CreateInstance(progId);
            if (shell == null) return null;
            shortcut = shell.GetType().InvokeMember("CreateShortcut",
                System.Reflection.BindingFlags.InvokeMethod,
                null, shell, new object[] { path });
            if (shortcut == null) return null;
            string? target = shortcut.GetType().InvokeMember("TargetPath",
                System.Reflection.BindingFlags.GetProperty,
                null, shortcut, null) as string;
            return target != null && Directory.Exists(target) ? target : null;
        }
        catch { return null; }
        finally
        {
            // Even COM cleanup can fail for broken shell extensions.
            try
            {
                if (shortcut != null && Marshal.IsComObject(shortcut))
                    Marshal.FinalReleaseComObject(shortcut);
            }
            catch (Exception ex) { Debug.WriteLine("JANUS shell link cleanup: " + ex); }
            try
            {
                if (shell != null && Marshal.IsComObject(shell))
                    Marshal.FinalReleaseComObject(shell);
            }
            catch (Exception ex) { Debug.WriteLine("JANUS shell cleanup: " + ex); }
        }
    }

    public static void OpenExplorer(string path)
    {
        try
        {
            var info = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
            info.ArgumentList.Add(path == "shell:MyComputerFolder"
                ? "shell:MyComputerFolder" : Resolve(path));
            Process.Start(info);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "JANUS Dock - Explorador",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void OpenFile(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "JANUS Dock - Archivo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static Page ReadPage(string path, int pageIndex, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            int folderOffset = checked(pageIndex * FolderPageSize);
            int fileOffset = checked(pageIndex * FilePageSize);
            var enumeration = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                ReturnSpecialDirectories = false,
                AttributesToSkip = 0
            };
            // Only 18 folders + 10 files on screen, even for folders
            // containing thousands of entries. Further pages are submenus.
            var folderPaths = Directory.EnumerateDirectories(path, "*", enumeration)
                .Skip(folderOffset).Take(FolderPageSize + 1).ToArray();
            token.ThrowIfCancellationRequested();
            var filePaths = Directory.EnumerateFiles(path, "*", enumeration)
                .Skip(fileOffset).Take(FilePageSize + 1).ToArray();
            token.ThrowIfCancellationRequested();

            var folders = folderPaths.Take(FolderPageSize)
                .Select(p => new Entry(p, System.IO.Path.GetFileName(p), true))
                .OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            var files = new List<Entry>();
            foreach (string file in filePaths.Take(FilePageSize))
            {
                token.ThrowIfCancellationRequested();
                var target = TryShortcutFolder(file);
                files.Add(target == null
                    ? new Entry(file, System.IO.Path.GetFileName(file), false)
                    : new Entry(target, System.IO.Path.GetFileNameWithoutExtension(file), true, true));
            }
            files.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name));
            return new Page(folders, files,
                folderPaths.Length > FolderPageSize || filePaths.Length > FilePageSize,
                null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            // Permission, intermittent network drives, inaccessible folders or
            // broken reparse points must never terminate the Dock.
            return new Page(new(), new(), false,
                "No se pudo leer esta carpeta: " + ex.Message);
        }
    }

    private static ToolStripMenuItem WaitItem() =>
        new("Cargando carpetas...") { Enabled = false };

    private static ToolStripMenuItem FolderItem(
        string path, string label, MenuSession session)
    {
        var item = new ToolStripMenuItem("📁  " + label) { ToolTipText = path };
        item.DropDownItems.Add(WaitItem());
        bool loaded = false, loading = false;
        item.DropDownOpening += (_, _) =>
        {
            if (loaded || loading || session.Closed || item.IsDisposed) return;
            loading = true;
            _ = Load();
        };

        async Task Load()
        {
            try
            {
                // A closed submenu is allowed to try again when reopened.
                bool result = await FillAsync(item.DropDownItems, path, 0, false,
                    session, () => !item.IsDisposed && item.DropDown.Visible);
                loaded = result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("JANUS folder submenu: " + ex);
            }
            finally { loading = false; }
        }
        return item;
    }

    private static ToolStripMenuItem NextPage(
        string path, int page, MenuSession session)
    {
        var item = new ToolStripMenuItem("Más carpetas y archivos…  ▶")
        { ToolTipText = "Página " + (page + 1) };
        item.DropDownItems.Add(WaitItem());
        bool loaded = false, loading = false;
        item.DropDownOpening += (_, _) =>
        {
            if (loaded || loading || session.Closed || item.IsDisposed) return;
            loading = true;
            _ = Load();
        };
        async Task Load()
        {
            try
            {
                loaded = await FillAsync(item.DropDownItems, path, page, false,
                    session, () => !item.IsDisposed && item.DropDown.Visible);
            }
            catch (Exception ex) { Debug.WriteLine("JANUS folder page: " + ex); }
            finally { loading = false; }
        }
        return item;
    }

    private static void FillItems(
        ToolStripItemCollection output, string path, Page page,
        int pageIndex, bool root, MenuSession session)
    {
        // Root keeps its fixed "Open in Explorer" + separator at positions 0/1.
        int keep = root ? 2 : 0;
        while (output.Count > keep) output.RemoveAt(keep);
        if (!root)
        {
            output.Add("Abrir esta carpeta en el Explorador", null,
                (_, _) => OpenExplorer(path));
            output.Add(new ToolStripSeparator());
        }
        if (page.Error != null)
        {
            output.Add(new ToolStripMenuItem(page.Error) { Enabled = false });
            return;
        }
        foreach (Entry entry in page.Folders)
            output.Add(FolderItem(entry.Path, entry.Name, session));
        if (page.Files.Count > 0 && page.Folders.Count > 0)
            output.Add(new ToolStripSeparator());
        foreach (Entry entry in page.Files)
        {
            if (entry.Folder)
                output.Add(FolderItem(entry.Path, "🔗  " + entry.Name, session));
            else
            {
                var file = new ToolStripMenuItem("📄  " + entry.Name)
                { ToolTipText = entry.Path };
                file.Click += (_, _) => OpenFile(entry.Path);
                output.Add(file);
            }
        }
        if (page.Folders.Count == 0 && page.Files.Count == 0)
            output.Add(new ToolStripMenuItem("(Carpeta vacía)") { Enabled = false });
        if (page.More)
            output.Add(NextPage(path, pageIndex + 1, session));
    }

    private static async Task<bool> FillAsync(
        ToolStripItemCollection target, string path, int pageIndex, bool root,
        MenuSession session, Func<bool> stillVisible)
    {
        try
        {
            Page data = await Task.Run(
                () => ReadPage(path, pageIndex, session.Token), session.Token);
            if (session.Closed || !stillVisible()) return false;
            // WinForms synchronization context is captured at the menu event,
            // so ToolStrip item changes happen only on the UI thread.
            FillItems(target, path, data, pageIndex, root, session);
            return true;
        }
        catch (OperationCanceledException) { return false; }
        catch (Exception ex)
        {
            // ToolStrip may be disposed mid-load, or a Windows shell extension
            // may throw when opening a dropdown. Log, never crash the Dock.
            Debug.WriteLine("JANUS folder menu: " + ex);
            return false;
        }
    }

    private static string DriveName(DriveInfo drive)
    {
        string letter = drive.Name.TrimEnd('\\');
        try
        {
            if (drive.IsReady && !string.IsNullOrWhiteSpace(drive.VolumeLabel))
                return drive.VolumeLabel + " (" + letter + ")";
        }
        catch { }
        string type = drive.DriveType switch
        {
            DriveType.Fixed => "Disco local",
            DriveType.Removable => "Unidad extraíble",
            DriveType.CDRom => "Unidad de CD/DVD",
            DriveType.Network => "Unidad de red",
            _ => "Unidad"
        };
        return type + " (" + letter + ")";
    }

    public static void Show(Control anchor, string path, string dockEdge)
    {
        try
        {
            string folder = Resolve(path);
            var session = new MenuSession();
            var menu = DockMenuStyle.Create(
                DockContrast.IsLight(anchor.BackColor),
                System.Drawing.Color.FromArgb(58, 173, 201));
            menu.AutoClose = true;
            menu.MaximumSize = new System.Drawing.Size(520, 660);
            menu.Closed += (_, _) =>
            {
                session.Close();
                // Do not dispose a ToolStrip in the middle of its own Closed
                // event. Defer disposal until WinForms finishes menu layout.
                try
                {
                    if (anchor.IsHandleCreated && !anchor.IsDisposed)
                        anchor.BeginInvoke(new Action(() =>
                        {
                            try { menu.Dispose(); }
                            catch (ObjectDisposedException) { }
                        }));
                    else
                        menu.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("JANUS menu close: " + ex);
                }
            };
            if (path == "shell:MyComputerFolder")
            {
                menu.Items.Add("Abrir Este equipo en el Explorador", null,
                    (_, _) => OpenExplorer("shell:MyComputerFolder"));
                menu.Items.Add(new ToolStripSeparator());
                try
                {
                    foreach (DriveInfo drive in DriveInfo.GetDrives()
                        .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                        menu.Items.Add(FolderItem(drive.RootDirectory.FullName,
                            DriveName(drive), session));
                }
                catch (Exception ex)
                {
                    menu.Items.Add(new ToolStripMenuItem(
                        "No se pueden listar las unidades: " + ex.Message)
                    { Enabled = false });
                }
            }
            else
            {
                menu.Items.Add("Abrir esta carpeta en el Explorador", null,
                    (_, _) => OpenExplorer(folder));
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(WaitItem());
            }

            var direction = dockEdge switch
            {
                "Abajo" => ToolStripDropDownDirection.AboveRight,
                "Arriba" => ToolStripDropDownDirection.BelowRight,
                "Derecha" => ToolStripDropDownDirection.Left,
                _ => ToolStripDropDownDirection.Right
            };
            menu.Show(anchor, new System.Drawing.Point(0, 0), direction);
            if (path != "shell:MyComputerFolder")
                _ = FillAsync(menu.Items, folder, 0, true, session,
                    () => !menu.IsDisposed && menu.Visible);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("JANUS folder menu could not open: " + ex);
            // Errors in optional browsing must never terminate the main bar.
        }
    }
}
