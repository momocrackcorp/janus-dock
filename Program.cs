using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace JanusDock;

public class DockItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public bool IsSeparator { get; set; } = false;
    public bool IsDiskWidget { get; set; } = false;
}
public class DockSettings
{
    public List<DockItem> Items { get; set; } = new();
    public string Language { get; set; } = "es";
    public int Width { get; set; } = 88;
    public int HorizontalHeight { get; set; } = 64;
    public int IconSize { get; set; } = 40;
    public int IconSpacing { get; set; } = 4;
    public string IconStyle { get; set; } = "Windows";
    public string BarColorMode { get; set; } = "Windows";
    public int CustomBarColorArgb { get; set; } = Color.FromArgb(36, 57, 64).ToArgb();
    public bool ShowNames { get; set; } = false;
    public string NameMode { get; set; } = "Tooltip";
    public string DockMode { get; set; } = "Acoplada";
    public string Edge { get; set; } = "Derecha";
    public bool AlwaysOnTop { get; set; } = true;
    public bool AutoHide { get; set; } = false;
    public bool ShowDiskWidget { get; set; } = false;
    public bool ShowNetworkTraffic { get; set; } = false;
    public bool ShowSystemUsage { get; set; } = false;
    public bool ShowCpu { get; set; } = true;
    public bool ShowRam { get; set; } = true;
    public string UsageLayout { get; set; } = "Vertical";
    public bool NetworkAtEnd { get; set; } = false;
    public int DiskColorArgb { get; set; } = Color.FromArgb(48, 130, 222).ToArgb();
    public int CriticalPercent { get; set; } = 90;
    public Dictionary<string,int> DiskColors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> HiddenDiskLetters { get; set; } = new();
    public bool HoverAnimation { get; set; } = true;
    public string HoverEffect { get; set; } = "Lupa";
    public int ZoomPercent { get; set; } = 280;
    public bool ShowLock { get; set; } = false;
    public bool ShowShutdown { get; set; } = false;
    public bool ShowRestart { get; set; } = false;
}
internal static class Program
{
    [STAThread] static void Main()
    {
        using var mutex = new Mutex(true, @"Local\JANUS_Dock_OnlyOne", out bool first);
        if (!first) return;
        ApplicationConfiguration.Initialize();
        Application.Run(new Dock());
    }
}
public class Dock : Form, IMessageFilter
{
    const int ABM_NEW = 0, ABM_REMOVE = 1, ABM_QUERYPOS = 2, ABM_SETPOS = 3;
    const int ABE_LEFT = 0, ABE_TOP = 1, ABE_RIGHT = 2, ABE_BOTTOM = 3;
    const int CallbackMessage = 0x8000 + 417;
    [StructLayout(LayoutKind.Sequential)]
    struct Rect { public int left, top, right, bottom; }
    [StructLayout(LayoutKind.Sequential)]
    struct AppBar
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uCallbackMessage, uEdge;
        public Rect rc;
        public IntPtr lParam;
    }
    [DllImport("shell32.dll")] static extern IntPtr SHAppBarMessage(int cmd, ref AppBar data);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHSTOCKICONINFO
    {
        public uint cbSize;
        public IntPtr hIcon;
        public int iSysImageIndex;
        public int iIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szPath;
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHGetStockIconInfo(int id, uint flags, ref SHSTOCKICONINFO info);
    const int SIID_DESKTOPPC = 94;
    const uint SHGSI_ICON = 0x100, SHGSI_LARGEICON = 0;
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern uint ExtractIconEx(string fileName, int iconIndex, IntPtr[]? largeIcons, IntPtr[]? smallIcons, uint icons);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr hIcon);
    [DllImport("user32.dll")] static extern bool LockWorkStation();
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    const int WM_NCLBUTTONDOWN = 0xA1, HTCAPTION = 2;
    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    class ShellLink { }
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
    interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder file, int max, IntPtr data, uint flags);
        void GetIDList(out IntPtr ptr);
        void SetIDList(IntPtr ptr);
        void GetDescription(IntPtr text, int max);
        void SetDescription(string text);
        void GetWorkingDirectory(IntPtr text, int max);
        void SetWorkingDirectory(string text);
        void GetArguments(IntPtr text, int max);
        void SetArguments(string text);
        void GetHotkey(out short hotkey);
        void SetHotkey(short hotkey);
        void GetShowCmd(out int command);
        void SetShowCmd(int command);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder icon, int max, out int index);
        void SetIconLocation(string path, int index);
        void SetRelativePath(string relative, uint reserved);
        void Resolve(IntPtr hwnd, uint flags);
        void SetPath(string path);
    }
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0000010B-0000-0000-C000-000000000046")]
    interface IPersistFile { void GetClassID(out Guid id); void IsDirty(); void Load([MarshalAs(UnmanagedType.LPWStr)] string file, uint mode); void Save([MarshalAs(UnmanagedType.LPWStr)] string file, bool remember); void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string file); void GetCurFile(out IntPtr file); }
    const uint SHGFI_ICON = 0x100, SHGFI_LARGEICON = 0, SHGFI_USEFILEATTRIBUTES = 0x10, FILE_ATTRIBUTE_NORMAL = 0x80;

    readonly string config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JANUS", "Dock", "config.json");
    readonly FlowLayoutPanel panel = new()
    {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
        WrapContents = false, AutoScroll = true, Padding = new Padding(0)
    };
    readonly RoundedEnergyPanel energyPanel = new()
    {
        FlowDirection = FlowDirection.TopDown, WrapContents = false,
        AutoScroll = false, Padding = new Padding(4), Visible = false
    };
    readonly Panel grip = new() { Dock = DockStyle.Left, Width = 7, Cursor = Cursors.SizeWE };
    readonly Panel footer = new() { Dock = DockStyle.Bottom, Height = 0, Visible = false };
    readonly Button add = new() { Text = "+", Width = 32, Height = 30, FlatStyle = FlatStyle.Flat };
    readonly Button options = new() { Text = "⚙", Width = 32, Height = 30, FlatStyle = FlatStyle.Flat };
    DockSettings settings = new();
    bool registered, repositioning, resizeActive, light;
    int dragX, dragWidth;
    readonly System.Windows.Forms.Timer themeTimer = new() { Interval = 3000 };
    readonly System.Windows.Forms.Timer hideTimer = new() { Interval = 400 };
    readonly ToolTip diskInfoTip = new() { AutomaticDelay = 250, AutoPopDelay = 12000 };
    bool collapsed;
    const int RevealStrip = 6;

    AppBar Data() => new()
    {
        cbSize = Marshal.SizeOf<AppBar>(), hWnd = Handle,
        uCallbackMessage = CallbackMessage, uEdge = settings.Edge switch { "Izquierda" => ABE_LEFT, "Arriba" => ABE_TOP, "Abajo" => ABE_BOTTOM, _ => ABE_RIGHT }
    };
    public Dock()
    {
        Application.AddMessageFilter(this);
        try { settings = JsonSerializer.Deserialize<DockSettings>(File.ReadAllText(config)) ?? new(); }
        catch { }
        settings.Language = DockLanguage.Normalize(settings.Language);
        settings.Width = Math.Clamp(settings.Width, 64, 300);
        settings.HorizontalHeight = Math.Clamp(settings.HorizontalHeight, 48, 160);
        settings.IconSize = Math.Clamp(settings.IconSize, 16, 96);
        settings.IconSpacing = Math.Clamp(settings.IconSpacing, 0, 24);
        settings.ZoomPercent = Math.Clamp(settings.ZoomPercent, 150, 340);
        // Migrate obsolete permanent-caption preference to tooltip-only labels.
        if (settings.NameMode == "Written" || settings.ShowNames) { settings.NameMode = "Tooltip"; settings.ShowNames = false; }
        if (settings.DockMode != "Flotante") settings.DockMode = "Acoplada";
        settings.CriticalPercent = Math.Clamp(settings.CriticalPercent, 70, 99);
        settings.DiskColors ??= new(StringComparer.OrdinalIgnoreCase);
        settings.HiddenDiskLetters ??= new();
        settings.UsageLayout = settings.UsageLayout == "Horizontal" ? "Horizontal" : "Vertical";
        // Migrate old disk widget into the reorderable item list once.
        if (settings.ShowDiskWidget && !settings.Items.Any(x => x.IsDiskWidget))
            settings.Items.Insert(0, new DockItem { Name = "Discos", IsDiskWidget = true });
        if (!new[] { "Derecha", "Izquierda", "Arriba", "Abajo" }.Contains(settings.Edge)) settings.Edge = "Derecha";
        // Existing manually added power items remain until the user changes the settings.
        Text = "JANUS Dock";
        Width = settings.Width;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = settings.AlwaysOnTop;
        AllowDrop = true;
        MinimumSize = new Size(64, 200);
        add.Click += (_, _) => AddDialog();
        options.Click += (_, _) => ShowOptions();
        add.FlatAppearance.BorderSize = 0;
        options.FlatAppearance.BorderSize = 0;
        footer.Controls.Add(add);
        footer.Controls.Add(options);
        Controls.Add(panel);
        Controls.Add(energyPanel);
        Controls.Add(footer);
        Controls.Add(grip);
        SetOrientation();
        panel.ContextMenuStrip = MakeDockMenu();
        grip.ContextMenuStrip = MakeDockMenu();
        ContextMenuStrip = MakeDockMenu();
        grip.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                resizeActive = true;
                dragX = Horizontal ? Cursor.Position.Y : Cursor.Position.X;
                dragWidth = Horizontal ? settings.HorizontalHeight : settings.Width;
                grip.Capture = true;
            }
        };
        grip.MouseMove += (_, _) =>
        {
            if (resizeActive)
            {
                int current = Horizontal ? Cursor.Position.Y : Cursor.Position.X;
                int sign = settings.Edge is "Izquierda" or "Arriba" ? 1 : -1;
                if (Horizontal) settings.HorizontalHeight = Math.Clamp(dragWidth + sign * (current - dragX), 48, 160);
                else settings.Width = Math.Clamp(dragWidth + sign * (current - dragX), 64, 300);
                Position();
            }
        };
        grip.MouseUp += (_, _) => FinishResize();
        grip.MouseCaptureChanged += (_, _) => { if (resizeActive) FinishResize(); };
        Resize += (_, _) => AlignFooter();
        DragEnter += EnterDrag;
        DragDrop += DropFiles;
        panel.AllowDrop = true;
        panel.DragEnter += EnterDrag;
        panel.DragDrop += DropFiles;
        light = IsLightTheme();
        ApplyTheme();
        Render();
        themeTimer.Tick += (_, _) =>
        {
            bool next = IsLightTheme();
            Color nextBarColor = settings.BarColorMode switch
            {
                "Custom" => Color.FromArgb(settings.CustomBarColorArgb),
                "Classic" => Color.FromArgb(192, 192, 192),
                _ => TaskbarColor()
            };
            if (next != light || BackColor.ToArgb() != nextBarColor.ToArgb())
            {
                light = next;
                ApplyTheme();
                Render();
            }
        };
        themeTimer.Start();
        hideTimer.Tick += (_, _) => UpdateAutoHide();
        hideTimer.Start();
    }
    void FinishResize()
    {
        if (!resizeActive) return;
        resizeActive = false;
        Store();
        Render();
        Position();
    }
    bool IsLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return Convert.ToInt32(key?.GetValue("SystemUsesLightTheme", 1)) != 0;
        }
        catch { return true; }
    }
    Color TaskbarColor()
    {
        // DWM system accent is not guaranteed to equal Windows 11 taskbar material.
        // Blend it subtly with the system light/dark surface for a closer match.
        Color surface = light ? Color.FromArgb(245, 246, 249) : Color.FromArgb(32, 34, 40);
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            int raw = Convert.ToInt32(key?.GetValue("ColorizationColor", 0));
            if (raw == 0) return surface;
            var accent = Color.FromArgb((raw >> 16) & 255, (raw >> 8) & 255, raw & 255);
            // Respect Windows "Show accent color on Start and taskbar" preference.
            using var personalization = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            bool useAccentOnTaskbar = Convert.ToInt32(personalization?.GetValue("ColorPrevalence", 0)) != 0;
            double mix = useAccentOnTaskbar ? 0.72 : (light ? 0.10 : 0.18);
            return Color.FromArgb((int)(surface.R*(1-mix)+accent.R*mix), (int)(surface.G*(1-mix)+accent.G*mix), (int)(surface.B*(1-mix)+accent.B*mix));
        }
        catch { return surface; }
    }
    void ApplyTheme()
    {
        BackColor = settings.BarColorMode switch
        {
            "Custom" => Color.FromArgb(settings.CustomBarColorArgb),
            "Classic" => Color.FromArgb(192, 192, 192),
            _ => TaskbarColor()
        };
        ForeColor = BackColor.GetBrightness() > .54f ? Color.FromArgb(28, 30, 35) : Color.FromArgb(241, 241, 244);
        footer.BackColor = BackColor;
        panel.BackColor = BackColor;
        energyPanel.BackColor = BackColor;
        energyPanel.HorizontalBar = Horizontal;
        energyPanel.CapsuleColor = settings.IconStyle == "Lucide"
            ? Color.FromArgb(225, 236, 243)
            : (BackColor.GetBrightness() > .54f ? ControlPaint.Dark(BackColor, .08f) : ControlPaint.Light(BackColor, .16f));
        energyPanel.Invalidate();
        grip.BackColor = light ? Color.FromArgb(210, 216, 226) : Color.FromArgb(66, 71, 82);
        foreach (var button in new[] { add, options })
        {
            button.BackColor = light ? Color.FromArgb(228, 233, 242) : Color.FromArgb(58, 62, 72);
            button.ForeColor = ForeColor;
        }
    }
    void AlignFooter()
    {
        int w = Horizontal ? 80 : Math.Max(64, Width - grip.Width);
        add.Location = new Point(Math.Max(5, (w - 72) / 2), 7);
        options.Location = new Point(add.Right + 6, 7);
    }
    void Store()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(config)!);
        string temp = config + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, config, true);
    }
    void Add(string path)
    {
        if (!path.StartsWith("janus:", StringComparison.OrdinalIgnoreCase) && path != "shell:MyComputerFolder" && !File.Exists(path) && !Directory.Exists(path) &&
            !(Uri.TryCreate(path, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http"))) return;
        string name = path switch { "shell:MyComputerFolder" => "Este equipo", "janus:documents" => "Documentos", "janus:downloads" => "Descargas", "janus:lock" => "Bloquear", "janus:shutdown" => "Apagar", "janus:restart" => "Reiniciar", "janus:settings" => "Configuración", _ => Path.GetFileNameWithoutExtension(path.TrimEnd('\\', '/')) };
        settings.Items.Add(new DockItem { Name = string.IsNullOrWhiteSpace(name) ? path : name, Path = path });
        Store(); Render();
    }
    void AddFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = "Elige tu carpeta de trabajo" };
        if (dialog.ShowDialog(this) == DialogResult.OK) Add(dialog.SelectedPath);
    }
    void AddDialog()
    {
        using var f = new OpenFileDialog { Title = "Agregar programa o acceso directo", Filter = "Archivos y accesos|*.exe;*.lnk;*.url;*.*" };
        if (f.ShowDialog(this) == DialogResult.OK) Add(f.FileName);
        else
        {
            string target = Microsoft.VisualBasic.Interaction.InputBox("Ruta de carpeta o dirección web:", "JANUS Dock", "");
            if (!string.IsNullOrWhiteSpace(target)) Add(target.Trim());
        }
    }
    void EnterDrag(object? sender, DragEventArgs e) =>
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
    void DropFiles(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
            foreach (string path in paths) Add(path);
    }
    Image ShellFolderIcon(string folder)
    {
        // Retrieve the actual known-folder icon when available; otherwise use
        // Windows' stock folder icon instead of the blank document icon.
        if (!Directory.Exists(folder))
        {
            var stock = new SHSTOCKICONINFO { cbSize = (uint)Marshal.SizeOf<SHSTOCKICONINFO>(), szPath = "" };
            if (SHGetStockIconInfo(3, SHGSI_ICON | SHGSI_LARGEICON, ref stock) == 0 && stock.hIcon != IntPtr.Zero)
            {
                try { using var icon = (Icon)Icon.FromHandle(stock.hIcon).Clone(); return icon.ToBitmap(); }
                finally { DestroyIcon(stock.hIcon); }
            }
        }
        IntPtr result = SHGetFileInfo(folder, FILE_ATTRIBUTE_NORMAL, out var info, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_LARGEICON);
        if (result != IntPtr.Zero && info.hIcon != IntPtr.Zero)
        {
            try { using var icon = (Icon)Icon.FromHandle(info.hIcon).Clone(); return icon.ToBitmap(); }
            finally { DestroyIcon(info.hIcon); }
        }
        var fallback = new SHSTOCKICONINFO { cbSize = (uint)Marshal.SizeOf<SHSTOCKICONINFO>(), szPath = "" };
        if (SHGetStockIconInfo(3, SHGSI_ICON | SHGSI_LARGEICON, ref fallback) == 0 && fallback.hIcon != IntPtr.Zero)
        {
            try { using var icon = (Icon)Icon.FromHandle(fallback.hIcon).Clone(); return icon.ToBitmap(); }
            finally { DestroyIcon(fallback.hIcon); }
        }
        return SystemIcons.Application.ToBitmap();
    }
    // Classic tools motif: crossed wrench and screwdriver, based on the user's reference.
    Image WindowsSettingsIcon()
    {
        var image = new Bitmap(96, 96);
        using var g = Graphics.FromImage(image);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var metal = new Pen(Color.FromArgb(190, 202, 212), 11f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        using var edge = new Pen(Color.FromArgb(68, 83, 96), 2f);
        using var gold = new Pen(Color.FromArgb(244, 184, 40), 13f) { StartCap = System.Drawing.Drawing2D.LineCap.Round };
        g.DrawLine(metal, 21, 76, 72, 24);
        g.DrawEllipse(metal, 62, 12, 19, 19);
        g.DrawLine(edge, 19, 78, 74, 22);
        g.DrawLine(gold, 26, 24, 72, 74);
        g.DrawLine(metal, 19, 17, 28, 26);
        g.DrawEllipse(edge, 65, 73, 10, 10);
        return image;
    }
    // Native Windows Segoe icon glyphs, rendered uniformly at the dock's icon size.
    // No custom PNG artwork or mismatched button dimensions.
    Image WindowsPowerGlyph(string action)
    {
        const int canvas = 96;
        var bmp = new Bitmap(canvas, canvas);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        string glyph = action switch
        {
            "janus:lock" => "\uE72E",     // Lock
            "janus:restart" => "\uE72C",  // Refresh
            _ => "\uE7E8"                 // Power
        };
        Color ink = settings.IconStyle == "Lucide" ? Color.FromArgb(23, 42, 51) : action switch
        {
            "janus:lock" => Color.FromArgb(225, 175, 45),
            "janus:restart" => Color.FromArgb(42, 183, 136),
            _ => Color.FromArgb(228, 72, 77)
        };
        using var font = new Font("Segoe MDL2 Assets", 62f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(ink);
        using var layout = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(glyph, font, brush, new RectangleF(0, 0, canvas, canvas), layout);
        return bmp;
    }
    Image GetIcon(string path)
    {
        // Use built-in Windows power glyphs; no third-party PNG assets.
        if (path is "janus:lock" or "janus:shutdown" or "janus:restart") return WindowsPowerGlyph(path);
        if (settings.IconStyle == "Lucide")
        {
            var lucide = LucideDockIcons.Load(path, light);
            if (lucide != null) return lucide;
        }
        if (path == "janus:documents") return ShellFolderIcon(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        if (path == "janus:settings") return WindowsSettingsIcon();
        if (path == "janus:downloads") return ShellFolderIcon(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
        // Prefer the native installed app executable for recognized Office/browser links.
        // This was the successful strategy in build #109.
        var knownApp = CommonApps.Catalog.FirstOrDefault(app =>
            app.Hints.Any(h => Path.GetFileNameWithoutExtension(path).Contains(h, StringComparison.OrdinalIgnoreCase)) ||
            Path.GetFileName(path).Equals(app.Exe, StringComparison.OrdinalIgnoreCase));
        if (knownApp != null)
        {
            string? installed = CommonApps.LocateExecutable(knownApp);
            if (installed != null)
            {
                try
                {
                    using var nativeIcon = Icon.ExtractAssociatedIcon(installed);
                    if (nativeIcon != null) return nativeIcon.ToBitmap();
                }
                catch { }
            }
        }
        // Resolve the underlying target/icon of .lnk rather than asking the shell for
        // the shortcut composite (which contains the arrow overlay).
        if (Path.GetExtension(path).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var obj = new ShellLink();
                try
                {
                    ((IPersistFile)obj).Load(path, 0);
                    var link = (IShellLinkW)obj;
                    var iconPath = new System.Text.StringBuilder(1024);
                    link.GetIconLocation(iconPath, iconPath.Capacity, out int iconIndex);
                    if (iconPath.Length > 0)
                    {
                        string specifiedIcon = Environment.ExpandEnvironmentVariables(iconPath.ToString().Trim('"'));
                        if (File.Exists(specifiedIcon))
                        {
                            // Shell links may point to indexed icons in DLL/EXE files.
                            // ExtractAssociatedIcon ignores this index and often produces
                            // the same tiny generic executable icon for multiple apps.
                            var handles = new IntPtr[1];
                            try
                            {
                                if (ExtractIconEx(specifiedIcon, iconIndex, handles, null, 1) > 0 && handles[0] != IntPtr.Zero)
                                {
                                    using var original = (Icon)Icon.FromHandle(handles[0]).Clone();
                                    return original.ToBitmap();
                                }
                            }
                            finally { if (handles[0] != IntPtr.Zero) DestroyIcon(handles[0]); }
                        }
                    }
                    var target = new System.Text.StringBuilder(1024);
                    link.GetPath(target, target.Capacity, IntPtr.Zero, 0);
                    if (target.Length > 0 && File.Exists(target.ToString()))
                    {
                        using var ico = Icon.ExtractAssociatedIcon(target.ToString());
                        if (ico != null) return ico.ToBitmap();
                    }
                }
                finally { Marshal.FinalReleaseComObject(obj); }
            }
            catch { }
        }
            // Ask Windows Shell to resolve the actual shortcut icon, including
            // packaged applications and shortcuts whose targets are not plain EXEs.
            try
            {
                IntPtr status = SHGetFileInfo(path, 0, out var shortcutInfo,
                    (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_LARGEICON);
                if (status != IntPtr.Zero && shortcutInfo.hIcon != IntPtr.Zero)
                {
                    try { using var icon = (Icon)Icon.FromHandle(shortcutInfo.hIcon).Clone(); return icon.ToBitmap(); }
                    finally { DestroyIcon(shortcutInfo.hIcon); }
                }
            }
            catch { }
        try
        {
            if (path.StartsWith("janus:", StringComparison.OrdinalIgnoreCase))
            {
                var stockId = path switch { "janus:documents" => 77, "janus:downloads" => 122, "janus:shutdown" => 27, "janus:restart" => 238, _ => 0 };
                var stockIcon = new SHSTOCKICONINFO { cbSize = (uint)Marshal.SizeOf<SHSTOCKICONINFO>(), szPath = "" };
                if (SHGetStockIconInfo(stockId, SHGSI_ICON | SHGSI_LARGEICON, ref stockIcon) == 0 && stockIcon.hIcon != IntPtr.Zero)
                {
                    try { using var icon = (Icon)Icon.FromHandle(stockIcon.hIcon).Clone(); return icon.ToBitmap(); }
                    finally { DestroyIcon(stockIcon.hIcon); }
                }
            }
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme.StartsWith("http"))
                return (Image)SystemIcons.Information.ToBitmap();
            if (path == "shell:MyComputerFolder")
            {
                // Use the Windows stock "This PC" icon, not explorer.exe's application icon.
                var stock = new SHSTOCKICONINFO { cbSize = (uint)Marshal.SizeOf<SHSTOCKICONINFO>(), szPath = "" };
                if (SHGetStockIconInfo(SIID_DESKTOPPC, SHGSI_ICON | SHGSI_LARGEICON, ref stock) == 0 && stock.hIcon != IntPtr.Zero)
                {
                    try { using var pcIcon = (Icon)Icon.FromHandle(stock.hIcon).Clone(); return pcIcon.ToBitmap(); }
                    finally { DestroyIcon(stock.hIcon); }
                }
            }
            string iconPath = path;
            IntPtr result = SHGetFileInfo(iconPath, FILE_ATTRIBUTE_NORMAL, out var info,
                (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_LARGEICON);
            if (result != IntPtr.Zero && info.hIcon != IntPtr.Zero)
            {
                try { using var icon = (Icon)Icon.FromHandle(info.hIcon).Clone(); return icon.ToBitmap(); }
                finally { DestroyIcon(info.hIcon); }
            }
        }
        catch { }
        return (Image)SystemIcons.Application.ToBitmap();
    }
    void Render()
    {
        panel.SuspendLayout();
        energyPanel.SuspendLayout();
        foreach (var existingEnergy in energyPanel.Controls.Cast<Control>().ToArray())
        {
            energyPanel.Controls.Remove(existingEnergy);
            existingEnergy.Dispose();
        }
        while (panel.Controls.Count > 0)
        {
            var control = panel.Controls[0];
            panel.Controls.RemoveAt(0);
            if (control is Button b) { b.Image?.Dispose(); (b.Tag as Image)?.Dispose(); }
            control.Dispose();
        }
        // Use a fixed, tight icon cell. Hover effects paint inside the cell and never reserve extra space.
        // Keep actual center-to-center spacing close to Windows taskbar even at 125–150% DPI.
        int width = Horizontal ? Math.Max(22, (int)Math.Round(settings.IconSize * 0.78)) : Math.Max(24, settings.Width - 18);
        int energySlot = Math.Max(24, settings.IconSize + 2);
        int powerCount = (settings.ShowLock ? 1 : 0) + (settings.ShowShutdown ? 1 : 0) + (settings.ShowRestart ? 1 : 0);
        energyPanel.Width = Horizontal ? Math.Max(1, powerCount) * (energySlot + settings.IconSpacing) + 24 + (settings.ShowNetworkTraffic && settings.NetworkAtEnd ? 185 : 0) : settings.Width;
        energyPanel.Height = Horizontal ? settings.HorizontalHeight : Math.Max(1, powerCount) * (energySlot + settings.IconSpacing) + 18 + (settings.ShowNetworkTraffic && settings.NetworkAtEnd ? 184 : 0);
        foreach (var item in settings.Items.ToList())
        {
            bool isEnergy = item.Path is "janus:lock" or "janus:shutdown" or "janus:restart";
            var destination = isEnergy ? energyPanel : panel;
            if (item.IsDiskWidget)
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (!drive.IsReady || drive.TotalSize <= 0) continue;
                        if (settings.HiddenDiskLetters.Contains(drive.Name.Substring(0, 2), StringComparer.OrdinalIgnoreCase)) continue;
                        var view = new DiskUsageControl(drive, Color.FromArgb(settings.DiskColors.TryGetValue(drive.Name.Substring(0,2).ToUpperInvariant(), out int individual) ? individual : settings.DiskColorArgb), settings.CriticalPercent, true)
                        {
                            Width = Horizontal ? Math.Max(48, settings.IconSize + 22) : Math.Max(36, settings.Width - 16),
                            Height = Math.Max(34, Math.Min(settings.IconSize + 18, Horizontal ? settings.HorizontalHeight - 4 : settings.Width - 8)),
                            IconPixels = settings.IconSize, UseLucide = settings.IconStyle == "Lucide",
                            ZoomEnabled = settings.HoverAnimation, ZoomFactor = settings.ZoomPercent / 100f,
                            BackColor = BackColor, ForeColor = ForeColor,
                            Margin = Horizontal ? new Padding(2, Math.Max(0, (settings.HorizontalHeight - Math.Max(34, Math.Min(settings.IconSize + 18, settings.HorizontalHeight - 4))) / 2), 2, 0) : new Padding(0, 1, 0, 1)
                        };
                        var diskMenu = DockMenuStyle.Create(light, Color.FromArgb(58, 173, 201));
                        diskMenu.Items.Add("Mover bloque de discos hacia arriba", null, (_, _) => Move(item, -1));
                        diskMenu.Items.Add("Mover bloque de discos hacia abajo", null, (_, _) => Move(item, 1));
                        diskMenu.Items.Add("Opciones...", null, (_, _) => ShowOptions());
                        view.ContextMenuStrip = diskMenu;
                        diskInfoTip.SetToolTip(view, $"Unidad {drive.Name.TrimEnd('\\')}\nCapacidad: {drive.TotalSize / 1073741824d:N1} GB\nUsado: {(drive.TotalSize - drive.TotalFreeSpace) / 1073741824d:N1} GB\nDisponible: {drive.TotalFreeSpace / 1073741824d:N1} GB");
                        view.Margin = Horizontal ? new Padding(2, 1, 2, 1) : new Padding(Math.Max(0, (settings.Width - 16 - view.Width) / 2), 1, 0, 1);
                        panel.Controls.Add(view);
                    }
                    catch { }
                }
                continue;
            }
            if (item.IsSeparator)
            {
                // A wide transparent hit area makes a thin separator practical to select.
                var sep = new Panel
                {
                    Width = Horizontal ? 18 : Math.Max(30, settings.Width - 25),
                    Height = Horizontal ? Math.Max(30, settings.Width - 25) : 18,
                    Margin = new Padding(4, 2, 4, 2),
                    BackColor = BackColor,
                    Cursor = Cursors.Hand
                };
                sep.Paint += (_, e) =>
                {
                    using var line = new Pen(light ? Color.FromArgb(170, 178, 190) : Color.FromArgb(105, 112, 125), 2);
                    if (Horizontal) e.Graphics.DrawLine(line, sep.Width / 2, 3, sep.Width / 2, sep.Height - 3);
                    else e.Graphics.DrawLine(line, 3, sep.Height / 2, sep.Width - 3, sep.Height / 2);
                };
                var separatorMenu = DockMenuStyle.Create(light, Color.FromArgb(58, 173, 201));
                separatorMenu.Items.Add("Quitar separador", null, (_, _) => { settings.Items.Remove(item); Store(); Render(); });
                separatorMenu.Items.Add("Subir", null, (_, _) => Move(item, -1));
                separatorMenu.Items.Add("Bajar", null, (_, _) => Move(item, 1));
                sep.ContextMenuStrip = separatorMenu;
                panel.Controls.Add(sep);
                continue;
            }
            var btn = new ZoomShortcutButton
            {
                Width = width,
                Height = settings.IconSize + (settings.NameMode == "Written" ? 40 : 12),
                SourceIcon = GetIcon(item.Path),
                BaseIconSize = settings.IconSize,
                ZoomEnabled = settings.HoverEffect == "Lupa",
                FocusEnabled = settings.HoverEffect == "Foco",
                ZoomFactor = settings.ZoomPercent / 100f,
                ShowCaption = settings.NameMode == "Written",
                ImageAlign = settings.NameMode == "Written" ? ContentAlignment.TopCenter : ContentAlignment.MiddleCenter,
                Text = settings.NameMode == "Written" ? item.Name : "",
                TextAlign = ContentAlignment.BottomCenter,
                TextImageRelation = TextImageRelation.ImageAboveText,
                AutoEllipsis = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = BackColor,
                ForeColor = ForeColor,
                Font = new Font("Segoe UI", 8),
                Margin = Horizontal ? new Padding(settings.IconSpacing / 2, 1, settings.IconSpacing - settings.IconSpacing / 2, 1) : new Padding(1, settings.IconSpacing / 2, 1, settings.IconSpacing - settings.IconSpacing / 2)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = light ? Color.FromArgb(225, 233, 244) : Color.FromArgb(62, 67, 77);
            btn.FlatAppearance.MouseDownBackColor = light ? Color.FromArgb(210, 222, 239) : Color.FromArgb(78, 83, 97);
            btn.Click += (_, _) => Open(item);

            var tips = new ToolTip { ShowAlways = true };
            if (settings.NameMode != "None") tips.SetToolTip(btn, item.Name + "\n" + item.Path);
            var context = DockMenuStyle.Create(light, Color.FromArgb(58, 173, 201));
            context.Items.Add("Abrir", null, (_, _) => Open(item));
            context.Items.Add("Subir", null, (_, _) => Move(item, -1));
            context.Items.Add("Bajar", null, (_, _) => Move(item, 1));
            context.Items.Add("Quitar acceso", null, (_, _) => { settings.Items.Remove(item); Store(); Render(); });
            context.Items.Add(new ToolStripSeparator());
            context.Items.Add("Opciones de la barra", null, (_, _) => ShowOptions());
            btn.ContextMenuStrip = context;
            btn.AllowDrop = true;
            btn.DragEnter += EnterDrag;
            btn.DragDrop += DropFiles;
            if (isEnergy)
            {
                // Keep the native/lucide bitmap square; button dimensions need not be.
                btn.ImageAlign = ContentAlignment.MiddleCenter;
                btn.BackColor = energyPanel.CapsuleColor;
                btn.FocusEnabled = settings.HoverEffect == "Foco";
                btn.Width = Horizontal ? width : Math.Max(24, settings.Width - 18);
                btn.Height = settings.IconSize + (settings.NameMode == "Written" ? 40 : 12);
                btn.Margin = Horizontal ? new Padding(settings.IconSpacing / 2, Math.Max(0, (energyPanel.Height - btn.Height) / 2), settings.IconSpacing - settings.IconSpacing / 2, 0) : new Padding(1, settings.IconSpacing / 2, 1, settings.IconSpacing - settings.IconSpacing / 2);
            }
            destination.Controls.Add(btn);
        }
        if (settings.ShowSystemUsage)
        {
            // The display direction is independent of the dock's position.
            // On narrow vertical docks, stack the readings to avoid clipping.
            bool sideBySide = settings.UsageLayout == "Horizontal" && settings.ShowCpu && settings.ShowRam;
            int usageWidth = Horizontal
                ? (sideBySide ? 214 : Math.Max(170, Math.Min(245, settings.IconSize * 5 + 25)))
                : Math.Max(56, settings.Width - 14);
            bool showSideBySide = sideBySide && usageWidth >= 200;
            var usage = new SystemUsageControl(Horizontal, settings.ShowCpu, settings.ShowRam, showSideBySide)
            {
                Width = usageWidth,
                Height = Horizontal
                    ? Math.Max(36, Math.Min(settings.HorizontalHeight - 8, 68))
                    : showSideBySide ? 44 : settings.ShowCpu && settings.ShowRam ? 148 : 82,
                BackColor = BackColor,
                ForeColor = ForeColor
            };
            var usageMenu = DockMenuStyle.Create(light, Color.FromArgb(58, 173, 201));
            var layoutMenu = new ToolStripMenuItem("Distribución CPU / RAM");
            foreach (string layout in new[] { "Vertical", "Horizontal" })
            {
                var choice = new ToolStripMenuItem(layout == "Horizontal"
                    ? "Horizontal (lado a lado)" : "Vertical (uno sobre otro)")
                {
                    Checked = settings.UsageLayout == layout
                };
                choice.Click += (_, _) =>
                {
                    if (settings.UsageLayout == layout) return;
                    settings.UsageLayout = layout;
                    Store();
                    Render();
                };
                layoutMenu.DropDownItems.Add(choice);
            }
            usageMenu.Items.Add(layoutMenu);
            usageMenu.Items.Add(new ToolStripSeparator());
            usageMenu.Items.Add("Opciones de CPU y RAM...", null, (_, _) => ShowOptions("Sistema"));
            usage.ContextMenuStrip = usageMenu;
            panel.Controls.Add(usage);
        }
        if (settings.ShowNetworkTraffic)
        {
            var network = new NetworkTrafficControl(Horizontal)
            {
                Width = Horizontal ? 170 : Math.Max(65, settings.Width - 14),
                BackColor = BackColor,
                ForeColor = ForeColor
            };
            var networkMenu = DockMenuStyle.Create(light, Color.FromArgb(58, 173, 201));
            networkMenu.Items.Add("Opciones del monitor de red...", null, (_, _) => ShowOptions("General"));
            network.ContextMenuStrip = networkMenu;
            if (settings.NetworkAtEnd)
            {
                // In WinForms docking the trailing section is independent of the
                // scrolling shortcuts. Place network first, then power commands.
                energyPanel.Controls.Add(network);
                energyPanel.Controls.SetChildIndex(network, 0);
            }
            else panel.Controls.Add(network);
        }
        energyPanel.Visible = energyPanel.Controls.Count > 0;
        energyPanel.ResumeLayout();
        panel.ResumeLayout();
        AlignFooter();
        if (IsHandleCreated && !resizeActive) Position();
    }
    ToolStripMenuItem DrivesMenu()
    {
        var menu = new ToolStripMenuItem("Agregar unidad de disco");
        foreach (var drive in DriveInfo.GetDrives())
        {
            string path = drive.Name;
            string label;
            try { label = drive.IsReady ? $"{drive.VolumeLabel} ({path.TrimEnd('\\')})" : $"Unidad {path.TrimEnd('\\')} (no disponible)"; }
            catch { label = path; }
            menu.DropDownItems.Add(label, null, (_, _) => Add(path));
        }
        return menu;
    }
    ContextMenuStrip MakeDockMenu()
    {
        var menu = DockMenuStyle.Create(light, Color.FromArgb(58, 173, 201));
        menu.Opening += (_, _) =>
        {
            menu.Items.Clear();
            var addMenu = new ToolStripMenuItem("Agregar");
            addMenu.DropDownItems.Add("Programa o acceso directo...", null, (_, _) => AddDialog());
            var common = new ToolStripMenuItem("Accesos comunes");
            foreach (string category in new[] { "Navegadores", "Office" })
            {
                var group = new ToolStripMenuItem(category);
                foreach (var app in CommonApps.Catalog.Where(a => a.Category == category))
                {
                    string? located = CommonApps.Locate(app);
                    var shortcut = new ToolStripMenuItem(app.Name + (located is null ? " (no instalado)" : ""));
                    shortcut.Enabled = located is not null;
                    if (located is not null)
                    {
                        string target = located;
                        shortcut.Click += (_, _) =>
                        {
                            if (!settings.Items.Any(i => string.Equals(i.Path, target, StringComparison.OrdinalIgnoreCase)))
                                Add(target);
                        };
                    }
                    group.DropDownItems.Add(shortcut);
                }
                common.DropDownItems.Add(group);
            }
            addMenu.DropDownItems.Add(common);
            addMenu.DropDownItems.Add(DrivesMenu());
            var sys = new ToolStripMenuItem("Carpetas del sistema");
            sys.DropDownItems.Add("Este equipo", null, (_, _) => Add("shell:MyComputerFolder"));
            sys.DropDownItems.Add("Mis documentos", null, (_, _) => Add("janus:documents"));
            sys.DropDownItems.Add("Descargas", null, (_, _) => Add("janus:downloads"));
            sys.DropDownItems.Add("Configuración de Windows", null, (_, _) => Add("janus:settings"));
            sys.DropDownItems.Add("Seleccionar otra carpeta...", null, (_, _) => AddFolder());
            addMenu.DropDownItems.Add(sys);
            addMenu.DropDownItems.Add("Separador", null, (_, _) => { settings.Items.Add(new DockItem { Name = "Separador", IsSeparator = true }); Store(); Render(); });
            menu.Items.Add(addMenu);
            var organize = new ToolStripMenuItem("Organizar barra");
            organize.DropDownItems.Add("Abrir organizador visual...", null, (_, _) => ShowOptions("Apariencia"));
            var manageSeparators = new ToolStripMenuItem("Administrar separadores");
            int number = 0;
            foreach (var separator in settings.Items.Where(i => i.IsSeparator).ToList())
            {
                number++;
                var current = separator;
                var submenu = new ToolStripMenuItem($"Separador {number}");
                submenu.DropDownItems.Add("Mover hacia arriba", null, (_, _) => Move(current, -1));
                submenu.DropDownItems.Add("Mover hacia abajo", null, (_, _) => Move(current, 1));
                submenu.DropDownItems.Add("Eliminar separador", null, (_, _) => { settings.Items.Remove(current); Store(); Render(); });
                manageSeparators.DropDownItems.Add(submenu);
            }
            manageSeparators.Enabled = number > 0;
            organize.DropDownItems.Add(manageSeparators);
            menu.Items.Add(organize);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Opciones...", null, (_, _) => ShowOptions());
            menu.Items.Add("Refrescar barra", null, (_, _) => RefreshDock());
            menu.Items.Add("Salir de JANUS Dock", null, (_, _) => Close());
        };
        return menu;
    }
    void ShowOptions(string? initialTab = null)
    {
        // Apply is a reversible preview until the user chooses Save and close.
        string originalJson = JsonSerializer.Serialize(settings);
        bool originalStartup = false;
        try
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            originalStartup = runKey?.GetValue("JANUS Dock") != null;
        }
        catch { /* A restricted registry must not prevent opening options. */ }
        using var dlg = new DockOptionsForm(settings, initialTab);
        dlg.ApplyRequested += ApplyDockOptions;
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            ApplyDockOptions(dlg);
        }
        else
        {
            // Restore every setting and the dock's appearance after Apply + Cancel/X/Escape.
            var initial = JsonSerializer.Deserialize<DockSettings>(originalJson);
            if (initial != null)
            {
                bool geometryChanged = settings.Edge != initial.Edge ||
                    settings.DockMode != initial.DockMode ||
                    settings.Width != initial.Width ||
                    settings.HorizontalHeight != initial.HorizontalHeight;
                if (geometryChanged) UnregisterBar();
                settings = initial;
                TopMost = settings.AlwaysOnTop;
                collapsed = false;
                try { DockOptionsForm.SetStartup(originalStartup); } catch { }
                if (geometryChanged) SetOrientation();
                if (geometryChanged || !registered) RegisterBar();
                Store();
                Render();
                if (geometryChanged || !registered) Position();
            }
        }
    }
    void ApplyDockOptions(DockOptionsForm dlg)
    {
        // Keep the AppBar reservation stable for ordinary visual/widget edits.
        // Re-registering on every checkbox change can shift the bar away from its edge.
        bool reDock = settings.DockMode != dlg.DockMode || settings.Edge != dlg.Edge ||
            settings.Width != dlg.BarWidth || settings.HorizontalHeight != dlg.BarHeight;
        if (reDock) UnregisterBar();
        settings.Language = dlg.Language;
        settings.AlwaysOnTop = dlg.AlwaysOnTop;
        settings.AutoHide = dlg.AutoHide;
        settings.ShowNames = dlg.ShowNames;
        settings.NameMode = dlg.NameMode;
        settings.DockMode = dlg.DockMode;
        settings.ShowDiskWidget = dlg.ShowDiskWidget;
        settings.ShowNetworkTraffic = dlg.ShowNetworkTraffic;
        settings.ShowSystemUsage = dlg.ShowSystemUsage;
        settings.ShowCpu = dlg.ShowCpu;
        settings.ShowRam = dlg.ShowRam;
        settings.UsageLayout = dlg.UsageLayout;
        settings.NetworkAtEnd = dlg.NetworkAtEnd;
        settings.DiskColorArgb = dlg.DiskColorArgb;
        settings.CriticalPercent = dlg.CriticalPercent;
        settings.DiskColors = dlg.DiskColors;
        settings.HiddenDiskLetters = dlg.HiddenDiskLetters;
        settings.HoverEffect = dlg.HoverEffect;
        settings.HoverAnimation = dlg.HoverEffect == "Lupa";
        settings.ZoomPercent = dlg.ZoomPercent;
        settings.ShowLock = dlg.ShowLock;
        settings.ShowShutdown = dlg.ShowShutdown;
        settings.ShowRestart = dlg.ShowRestart;
        settings.Items = dlg.OrderedItems;
        SyncPowerButtons();
        var currentWidget = settings.Items.FirstOrDefault(i => i.IsDiskWidget);
        if (settings.ShowDiskWidget && currentWidget == null) settings.Items.Add(new DockItem { Name = "Discos", IsDiskWidget = true });
        if (!settings.ShowDiskWidget && currentWidget != null) settings.Items.Remove(currentWidget);
        settings.IconStyle = dlg.IconStyle;
        settings.BarColorMode = dlg.BarColorMode;
        settings.CustomBarColorArgb = dlg.CustomBarColorArgb;
        settings.IconSize = dlg.IconSize;
        settings.IconSpacing = dlg.IconSpacing;
        settings.Width = dlg.BarWidth;
        settings.HorizontalHeight = dlg.BarHeight;
        settings.Edge = dlg.Edge;
        TopMost = settings.AlwaysOnTop;
        collapsed = false;
        DockOptionsForm.SetStartup(dlg.StartWithWindows);
        if (reDock) SetOrientation();
        if (reDock || !registered) RegisterBar();
        Store();
        ApplyTheme();
        Render();
        if (reDock || !registered) Position();
    }
    void ShowLegacyOptions()
    {
        var menu = DockMenuStyle.Create(light, Color.FromArgb(58, 173, 201));
        var names = new ToolStripMenuItem("Mostrar nombres") { Checked = settings.ShowNames, CheckOnClick = true };
        names.CheckedChanged += (_, _) => { settings.ShowNames = names.Checked; Store(); Render(); };
        menu.Items.Add(names);
        menu.Items.Add("Agregar acceso...", null, (_, _) => AddDialog());
        menu.Items.Add("Agregar separador", null, (_, _) => { settings.Items.Add(new DockItem { Name = "Separador", IsSeparator = true }); Store(); Render(); });
        var locations = new ToolStripMenuItem("Ubicación");
        foreach (string edge in new[] { "Derecha", "Izquierda", "Arriba", "Abajo" })
        {
            var opt = new ToolStripMenuItem(edge) { Checked = settings.Edge == edge };
            opt.Click += (_, _) => { if (settings.Edge != edge) { UnregisterBar(); settings.Edge = edge; SetOrientation(); RegisterBar(); Store(); Render(); } };
            locations.DropDownItems.Add(opt);
        }
        menu.Items.Add(locations);
        var sizes = new ToolStripMenuItem("Tamaño de iconos");
        foreach (int size in new[] { 24, 32, 40, 48, 56, 64, 72, 80, 88, 96 })
        {
            var option = new ToolStripMenuItem(size + " px") { Checked = settings.IconSize == size };
            option.Click += (_, _) => { settings.IconSize = size; Store(); Render(); };
            sizes.DropDownItems.Add(option);
        }
        menu.Items.Add(sizes);
        menu.Items.Add("Refrescar barra", null, (_, _) => RefreshDock());
        menu.Items.Add("Cerrar JANUS Dock", null, (_, _) => Close());
        menu.Show(this, PointToClient(Cursor.Position));
    }
    void SyncPowerButtons()
    {
        // Old manually inserted power shortcuts are managed by these switches.
        foreach (var (path, enabled) in new[] { ("janus:lock", settings.ShowLock), ("janus:shutdown", settings.ShowShutdown), ("janus:restart", settings.ShowRestart) })
        {
            var existing = settings.Items.Where(x => x.Path == path).ToList();
            if (!enabled) foreach (var entry in existing) settings.Items.Remove(entry);
            else if (existing.Count == 0)
                settings.Items.Add(new DockItem { Path = path, Name = path == "janus:lock" ? "Bloquear" : path == "janus:shutdown" ? "Apagar" : "Reiniciar" });
            else foreach (var duplicate in existing.Skip(1)) settings.Items.Remove(duplicate);
        }
    }
    void RefreshDock()
    {
        light = IsLightTheme(); ApplyTheme();
        collapsed = false;
        UnregisterBar();
        RegisterBar();
        Render();
        Position();
    }
    void Move(DockItem item, int delta)
    {
        int i = settings.Items.IndexOf(item), n = i + delta;
        if (i < 0 || n < 0 || n >= settings.Items.Count) return;
        settings.Items.RemoveAt(i); settings.Items.Insert(n, item); Store(); Render();
    }
    void Open(DockItem item)
    {
        try
        {
            if (item.Path is "janus:shutdown" or "janus:restart")
            {
                string verb = item.Path == "janus:shutdown" ? "apagar" : "reiniciar";
                if (MessageBox.Show($"¿Seguro que quieres {verb} el equipo? Guarda tus documentos primero.", "Confirmar acción de energía", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                Process.Start(new ProcessStartInfo("shutdown.exe", item.Path == "janus:shutdown" ? "/s /t 0" : "/r /t 0") { UseShellExecute = true });
            }
            else if (item.Path == "janus:lock") { if (MessageBox.Show("¿Bloquear esta sesión?", "JANUS Dock", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) LockWorkStation(); }
            else if (item.Path == "janus:settings") Process.Start(new ProcessStartInfo("ms-settings:") { UseShellExecute = true });
            else if (item.Path == "janus:documents") Process.Start(new ProcessStartInfo("explorer.exe", $"\"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\"") { UseShellExecute = true });
            else if (item.Path == "janus:downloads") Process.Start(new ProcessStartInfo("explorer.exe", $"\"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")}\"") { UseShellExecute = true });
            else if (item.Path == "shell:MyComputerFolder") Process.Start(new ProcessStartInfo("explorer.exe", "shell:MyComputerFolder") { UseShellExecute = true });
            else Process.Start(new ProcessStartInfo(item.Path) { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo abrir", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    bool Horizontal => settings.Edge == "Arriba" || settings.Edge == "Abajo";
    void SetOrientation()
    {
        panel.FlowDirection = Horizontal ? FlowDirection.LeftToRight : FlowDirection.TopDown;
        energyPanel.FlowDirection = Horizontal ? FlowDirection.LeftToRight : FlowDirection.TopDown;
        energyPanel.HorizontalBar = Horizontal;
        energyPanel.Padding = Horizontal ? new Padding(16, 0, 6, 0) : new Padding(0, 7, 0, 3);
        energyPanel.Dock = Horizontal ? DockStyle.Right : DockStyle.Bottom;
        // A dedicated end zone leaves power controls far from ordinary shortcuts.
        energyPanel.BringToFront();
        panel.Dock = DockStyle.Fill;
        footer.Dock = Horizontal ? DockStyle.Right : DockStyle.Bottom;
        footer.Width = Horizontal ? 80 : settings.Width;
        footer.Height = 0;
        footer.Visible = false;
        MinimumSize = Horizontal ? new Size(200, 1) : new Size(64, 200);
        grip.Dock = settings.Edge switch
        {
            "Izquierda" => DockStyle.Right,
            "Arriba" => DockStyle.Bottom,
            "Abajo" => DockStyle.Top,
            _ => DockStyle.Left
        };
        grip.Cursor = Horizontal ? Cursors.SizeNS : Cursors.SizeWE;
        grip.Visible = settings.Edge != "Abajo" && settings.DockMode != "Flotante";
        FormBorderStyle = FormBorderStyle.None;
        if (settings.DockMode == "Flotante")
        {
            MinimumSize = new Size(65, 65);
            panel.MouseDown -= DragFloating;
            panel.MouseDown += DragFloating;
        }
        else panel.MouseDown -= DragFloating;
        AlignFooter();
    }
    void DragFloating(object? sender, MouseEventArgs e)
    {
        if (settings.DockMode != "Flotante" || e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, new IntPtr(HTCAPTION), IntPtr.Zero);
    }
    void UnregisterBar()
    {
        if (!registered) return;
        var d = Data();
        SHAppBarMessage(ABM_REMOVE, ref d);
        registered = false;
    }
    void RegisterBar()
    {
        if (!IsHandleCreated) return;
        // Register every docked orientation, including bottom. Without ABM_SETPOS
        // Windows cannot reserve working-area space above a horizontal dock.
        if (settings.DockMode == "Flotante") { registered = false; Position(); return; }
        var d = Data();
        registered = SHAppBarMessage(ABM_NEW, ref d) != IntPtr.Zero;
        Position();
    }
    void UpdateAutoHide()
    {
        if (settings.DockMode == "Flotante" || !settings.AutoHide || !Visible || resizeActive)
        {
            if (collapsed) { collapsed = false; Position(); }
            return;
        }
        var mouse = Cursor.Position;
        bool shouldExpand = Bounds.Contains(mouse);
        if (collapsed)
        {
            var screen = Screen.PrimaryScreen?.Bounds ?? Screen.FromControl(this).Bounds;
            shouldExpand = settings.Edge switch
            {
                "Derecha" => mouse.X >= screen.Right - 12,
                "Izquierda" => mouse.X <= screen.Left + 12,
                "Arriba" => mouse.Y <= screen.Top + 12,
                _ => mouse.Y >= (Screen.PrimaryScreen?.WorkingArea.Bottom ?? screen.Bottom) - 12 && mouse.Y < (Screen.PrimaryScreen?.WorkingArea.Bottom ?? screen.Bottom)
            };
        }
        if (shouldExpand == collapsed) { collapsed = !collapsed; Position(); }
    }
    void Position()
    {
        if (repositioning) return;
        repositioning = true;
        try
        {
            var bounds = Screen.PrimaryScreen?.Bounds ?? Screen.FromControl(this).Bounds;
            if (settings.DockMode == "Flotante")
            {
                panel.Visible = !collapsed;
                if (Width < 65 || Height < 65 || (Left == 0 && Top == 0)) Bounds = new Rectangle(bounds.Left + 140, bounds.Top + 120, 110, 450);
                return;
            }
            int thickness = collapsed ? RevealStrip : (Horizontal ? settings.HorizontalHeight : settings.Width);
            var d = Data();
            // Query from physical screen bounds, not WorkingArea: the latter
            // already includes our own AppBar reservation and would drift upward
            // on every layout refresh. ABM_QUERYPOS negotiates with the taskbar.
            d.rc = new Rect
            {
                left = settings.Edge == "Derecha" ? bounds.Right - thickness : bounds.Left,
                right = settings.Edge == "Izquierda" ? bounds.Left + thickness : bounds.Right,
                top = settings.Edge == "Abajo" ? bounds.Bottom - thickness : bounds.Top,
                bottom = settings.Edge == "Arriba" ? bounds.Top + thickness : bounds.Bottom
            };
            if (registered)
            {
                SHAppBarMessage(ABM_QUERYPOS, ref d);
                switch (settings.Edge)
                {
                    case "Izquierda": d.rc.right = d.rc.left + thickness; break;
                    case "Arriba": d.rc.bottom = d.rc.top + thickness; break;
                    case "Abajo": d.rc.top = d.rc.bottom - thickness; break;
                    default: d.rc.left = d.rc.right - thickness; break;
                }
                SHAppBarMessage(ABM_SETPOS, ref d);
            }
            Bounds = Rectangle.FromLTRB(d.rc.left, d.rc.top, d.rc.right, d.rc.bottom);
            panel.Visible = !collapsed;
            footer.Width = Horizontal ? 80 : settings.Width;
            // Do not overwrite the compact sizes assigned by Render().
            // In particular, AppBar positioning must not expand icon cells to
            // the hover-zoom size (that caused the docked-only spacing bug).
            // Only the dock window is repositioned here; controls keep their sizes.

        }
        finally { repositioning = false; }
    }
    // Ctrl + mouse wheel over the dock adjusts icons without opening Options.
    // The message filter also sees wheel events sent to shortcut/disk child controls.
    public bool PreFilterMessage(ref Message m)
    {
        const int WM_MOUSEWHEEL = 0x020A;
        if (m.Msg != WM_MOUSEWHEEL || IsDisposed || !Visible ||
            !Bounds.Contains(Cursor.Position)) return false;
        // Suppress WinForms' unexpected horizontal dock scrolling.
        // Ctrl+wheel remains reserved for icon size changes.
        if ((Control.ModifierKeys & Keys.Control) == 0) return true;
        if ((Control.ModifierKeys & (Keys.Shift | Keys.Alt)) != 0) return true;
        int delta = unchecked((short)((m.WParam.ToInt64() >> 16) & 0xFFFF));
        if (delta == 0) return false;
        int updated = Math.Clamp(settings.IconSize + (delta > 0 ? 4 : -4), 16, 96);
        if (updated != settings.IconSize)
        {
            settings.IconSize = updated;
            Store();
            Render();
            // Keep AppBar registration intact: only visual/icon dimensions change.
            Position();
        }
        return true;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SetOrientation();
        RegisterBar();
    }
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (registered && !resizeActive && (m.Msg == CallbackMessage || m.Msg == 0x007e)) Position();
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Application.RemoveMessageFilter(this);
        themeTimer.Stop();
        hideTimer.Stop();
        UnregisterBar();
        base.OnFormClosing(e);
    }
}
