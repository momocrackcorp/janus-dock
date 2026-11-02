using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>
/// A cascading folder/drive browser launched from any folder or "Este equipo"
/// Dock shortcut. Children are loaded only when opened, off the UI thread:
/// protected/slow folders cannot freeze the Dock while being enumerated.
/// </summary>
internal static class DockFolderTreeMenu
{
    private const int MaxFolders = 90;
    private const int MaxFiles = 35;

    private sealed record Entry(string FullPath, string Name, bool IsDirectory);
    private sealed record Listing(
        List<Entry> Folders, List<Entry> Files, bool Truncated, string? Error);

    public static bool CanBrowse(string path) =>
        path is "shell:MyComputerFolder" or "janus:documents" or "janus:downloads"
        || Directory.Exists(path);

    private static string Resolve(string path) => path switch
    {
        "janus:documents" => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "janus:downloads" => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
        _ => path
    };

    public static void OpenExplorer(string path)
    {
        try
        {
            if (path == "shell:MyComputerFolder")
                Process.Start(new ProcessStartInfo("explorer.exe", "shell:MyComputerFolder")
                { UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo("explorer.exe")
                { UseShellExecute = true, ArgumentList = { path } });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "JANUS Dock - Explorador",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void OpenFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "JANUS Dock - Archivo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static Listing Enumerate(string path)
    {
        try
        {
            // Limit enumeration to prevent unexpectedly huge folders and
            // keep the menu responsive even inside Program Files or AppData.
            var folders = Directory.EnumerateDirectories(path)
                .Take(MaxFolders + 1).ToArray();
            var files = Directory.EnumerateFiles(path)
                .Take(MaxFiles + 1).ToArray();
            bool more = folders.Length > MaxFolders || files.Length > MaxFiles;
            return new Listing(
                folders.Take(MaxFolders)
                    .Select(p => new Entry(p, Path.GetFileName(p), true))
                    .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
                files.Take(MaxFiles)
                    .Select(p => new Entry(p, Path.GetFileName(p), false))
                    .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
                more, null);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException
            or IOException or System.Security.SecurityException
            or ArgumentException or NotSupportedException)
        {
            return new Listing(new(), new(), false,
                "No se puede leer esta carpeta: " + ex.Message);
        }
    }

    private static ToolStripMenuItem Loading() =>
        new("Cargando carpetas...") { Enabled = false };

    private static ToolStripMenuItem Folder(string path, string name)
    {
        var item = new ToolStripMenuItem("📁  " + name)
        {
            ToolTipText = path
        };
        // Ensure a submenu arrow exists even before disk enumeration.
        item.DropDownItems.Add(Loading());
        bool loaded = false;
        item.DropDownOpening += async (_, _) =>
        {
            if (loaded) return;
            loaded = true;
            await PopulateAsync(item.DropDownItems, path, () => item.IsDisposed);
        };
        return item;
    }

    private static async Task PopulateAsync(
        ToolStripItemCollection output, string path, Func<bool> disposed)
    {
        Listing listing = await Task.Run(() => Enumerate(path));
        if (disposed()) return;

        // Parent item or menu may be closed while an inaccessible drive loads.
        try
        {
            output.Clear();
            var open = new ToolStripMenuItem("Abrir esta carpeta en el Explorador");
            open.Click += (_, _) => OpenExplorer(path);
            output.Add(open);
            output.Add(new ToolStripSeparator());

            if (listing.Error != null)
            {
                output.Add(new ToolStripMenuItem(listing.Error) { Enabled = false });
                return;
            }

            foreach (Entry folder in listing.Folders)
                output.Add(Folder(folder.FullPath, folder.Name));

            if (listing.Files.Count > 0 && listing.Folders.Count > 0)
                output.Add(new ToolStripSeparator());

            foreach (Entry file in listing.Files)
            {
                var fileItem = new ToolStripMenuItem("📄  " + file.Name)
                { ToolTipText = file.FullPath };
                fileItem.Click += (_, _) => OpenFile(file.FullPath);
                output.Add(fileItem);
            }

            if (listing.Folders.Count == 0 && listing.Files.Count == 0)
                output.Add(new ToolStripMenuItem("(Carpeta vacía)") { Enabled = false });

            if (listing.Truncated)
            {
                output.Add(new ToolStripSeparator());
                var more = new ToolStripMenuItem("Ver más en el Explorador...");
                more.Click += (_, _) => OpenExplorer(path);
                output.Add(more);
            }
        }
        catch (ObjectDisposedException)
        {
            // Menus can be dismissed before a slow drive finishes loading.
        }
    }

    public static void Show(Control anchor, string path, string dockEdge)
    {
        string folder = Resolve(path);
        var menu = DockMenuStyle.Create(
            DockContrast.IsLight(anchor.BackColor),
            System.Drawing.Color.FromArgb(58, 173, 201));
        menu.ShowImageMargin = false;
        menu.AutoClose = true;

        if (path == "shell:MyComputerFolder")
        {
            menu.Items.Add("Abrir Este equipo en el Explorador", null,
                (_, _) => OpenExplorer("shell:MyComputerFolder"));
            menu.Items.Add(new ToolStripSeparator());

            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives()
                    .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                {
                    var d = drive;
                    var entry = Folder(d.RootDirectory.FullName,
                        d.Name.TrimEnd('\\') + "  " + d.DriveType);
                    menu.Items.Add(entry);
                }
            }
            catch (Exception ex) when (ex is IOException
                or UnauthorizedAccessException or System.Security.SecurityException)
            {
                menu.Items.Add(new ToolStripMenuItem("No se pueden enumerar las unidades")
                { Enabled = false });
            }
        }
        else
        {
            menu.Items.Add("Abrir esta carpeta en el Explorador", null,
                (_, _) => OpenExplorer(folder));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Loading());
        }

        menu.Closed += (_, _) => menu.Dispose();
        var direction = dockEdge switch
        {
            "Abajo" => ToolStripDropDownDirection.AboveRight,
            "Arriba" => ToolStripDropDownDirection.BelowRight,
            "Derecha" => ToolStripDropDownDirection.Left,
            _ => ToolStripDropDownDirection.Right
        };
        menu.Show(anchor, new System.Drawing.Point(0, 0), direction);

        if (path != "shell:MyComputerFolder")
            _ = PopulateRootAsync(menu, folder);
    }

    private static async Task PopulateRootAsync(ContextMenuStrip menu, string folder)
    {
        Listing listing = await Task.Run(() => Enumerate(folder));
        if (menu.IsDisposed || !menu.Visible) return;
        try
        {
            // Keep the direct "Open in Explorer" entry at the top.
            while (menu.Items.Count > 2)
                menu.Items.RemoveAt(2);

            if (listing.Error != null)
            {
                menu.Items.Add(new ToolStripMenuItem(listing.Error) { Enabled = false });
                return;
            }
            foreach (Entry entry in listing.Folders)
                menu.Items.Add(Folder(entry.FullPath, entry.Name));
            if (listing.Files.Count > 0 && listing.Folders.Count > 0)
                menu.Items.Add(new ToolStripSeparator());
            foreach (Entry entry in listing.Files)
            {
                var file = new ToolStripMenuItem("📄  " + entry.Name)
                { ToolTipText = entry.FullPath };
                file.Click += (_, _) => OpenFile(entry.FullPath);
                menu.Items.Add(file);
            }
            if (listing.Folders.Count == 0 && listing.Files.Count == 0)
                menu.Items.Add(new ToolStripMenuItem("(Carpeta vacía)") { Enabled = false });
            if (listing.Truncated)
            {
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Ver más en el Explorador...", null,
                    (_, _) => OpenExplorer(folder));
            }
        }
        catch (ObjectDisposedException) { }
    }
}
