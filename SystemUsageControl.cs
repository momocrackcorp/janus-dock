using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>System-wide CPU and physical RAM metrics, updated once per second.</summary>
public sealed class SystemUsageControl : Control
{
    [StructLayout(LayoutKind.Sequential)]
    struct FileTime { public uint Low; public uint High; public ulong Value => ((ulong)High << 32) | Low; }
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);
    [StructLayout(LayoutKind.Sequential)]
    struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys, AvailPhys, TotalPageFile, AvailPageFile, TotalVirtual, AvailVirtual, AvailExtendedVirtual;
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    readonly ToolTip infoTip = new() { InitialDelay = 350, AutoPopDelay = 12000 };
    ulong lastIdle, lastTotal;
    bool initialized;
    double cpuPercent, memoryPercent, usedGiB, totalGiB;
    public bool Horizontal { get; }
    public bool SideBySide { get; }
    public string UsageStyle { get; }
    readonly bool showCpu, showRam;
    public SystemUsageControl(bool horizontal, bool cpuVisible = true, bool ramVisible = true,
        bool sideBySide = false, string usageStyle = "Clásico")
    {
        Horizontal = horizontal;
        SideBySide = sideBySide;
        UsageStyle = usageStyle;
        showCpu = cpuVisible; showRam = ramVisible;
        Size = horizontal ? new Size(sideBySide && showCpu && showRam ? 160 : 120, sideBySide && showCpu && showRam ? 42 : showCpu && showRam ? 56 : 36) : new Size(88, showCpu && showRam ? 150 : 82);
        Margin = new Padding(4, 2, 4, 2);
        DoubleBuffered = true;
        timer.Tick += (_, _) => Sample();
        Sample();
        timer.Start();
    }
    void Sample()
    {
        try
        {
            if (GetSystemTimes(out var idle, out var kernel, out var user))
            {
                ulong total = kernel.Value + user.Value;
                if (initialized && total >= lastTotal && idle.Value >= lastIdle)
                {
                    ulong interval = total - lastTotal;
                    if (interval > 0)
                        cpuPercent = Math.Clamp(100d * (interval - Math.Min(interval, idle.Value - lastIdle)) / interval, 0, 100);
                }
                initialized = true; lastTotal = total; lastIdle = idle.Value;
            }
            var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
            if (GlobalMemoryStatusEx(ref memory) && memory.TotalPhys > 0)
            {
                totalGiB = memory.TotalPhys / 1073741824d;
                usedGiB = (memory.TotalPhys - memory.AvailPhys) / 1073741824d;
                memoryPercent = 100d * usedGiB / totalGiB;
            }
            AccessibleDescription = $"CPU {cpuPercent:0}% | RAM {memoryPercent:0}% ({usedGiB:0.0} de {totalGiB:0.0} GB)";
            infoTip.SetToolTip(this, $"CPU: {cpuPercent:0}%\nRAM usada: {usedGiB:0.0} GB\nRAM total: {totalGiB:0.0} GB\nRAM disponible: {totalGiB - usedGiB:0.0} GB");
            Invalidate();
        }
        catch { /* Keep last valid sample. */ }
    }
    private void PaintCircular(Graphics g)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        bool two = showCpu && showRam;
        bool across = Horizontal && two;
        float vw = across ? 140f : Horizontal ? 74f : 86f;
        float vh = across || Horizontal ? 64f : two ? 130f : 70f;
        float factor = Math.Min(Width / vw, Height / vh);
        if (factor <= 0) return;
        g.TranslateTransform((Width - vw * factor) / 2, (Height - vh * factor) / 2);
        g.ScaleTransform(factor, factor);
        using var baseRing = new Pen(Color.FromArgb(64, 79, 105), 5.3f)
        { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        using var cpuRing = new Pen(Color.FromArgb(255, 120, 53), 5.3f)
        { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        using var ramRing = new Pen(Color.FromArgb(67, 146, 255), 5.3f)
        { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        using var labelFont = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var glyph = new Pen(Color.FromArgb(219, 226, 240), 1.7f);
        using var textBrush = new SolidBrush(Color.FromArgb(221, 229, 239));
        using var centered = new StringFormat
        { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        void Meter(float x, float y, double percent, bool cpu)
        {
            var ring = new RectangleF(x - 20, y - 20, 40, 40);
            g.DrawArc(baseRing, ring, 0, 359.9f);
            g.DrawArc(cpu ? cpuRing : ramRing, ring, -90, (float)Math.Clamp(percent * 3.6, 0, 359.9));
            if (cpu)
            {
                g.DrawRectangle(glyph, x - 8, y - 6, 16, 13);
                g.DrawLine(glyph, x - 4, y - 9, x - 4, y - 6);
                g.DrawLine(glyph, x + 4, y - 9, x + 4, y - 6);
            }
            else
            {
                g.DrawRectangle(glyph, x - 10, y - 5, 20, 10);
                for (int i = -6; i <= 6; i += 6)
                    g.DrawLine(glyph, x + i, y + 5, x + i, y + 9);
            }
            g.DrawString($"{percent:0}%", labelFont, textBrush,
                new RectangleF(x - 26, y + 21, 52, 16), centered);
        }
        if (across)
        {
            Meter(35, 25, cpuPercent, true);
            Meter(105, 25, memoryPercent, false);
        }
        else if (Horizontal)
        {
            Meter(37, 25, showCpu ? cpuPercent : memoryPercent, showCpu);
        }
        else
        {
            if (showCpu) Meter(43, 28, cpuPercent, true);
            if (showRam) Meter(43, showCpu ? 92 : 28, memoryPercent, false);
        }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        if (UsageStyle == "Circular")
        {
            PaintCircular(g);
            return;
        }
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        // Draw inside a normalized coordinate system, preserving icon proportions
        // even when Windows display scaling or dock thickness changes.
        bool horizontal = Horizontal;
        bool twoAcross = SideBySide && showCpu && showRam && Width >= 154;
        float virtualWidth = twoAcross ? 156f : horizontal ? 120f : 90f;
        float virtualHeight = twoAcross ? 36f : horizontal ? (showCpu && showRam ? 70f : 36f) : (showCpu && showRam ? 150f : 82f);
        float scale = Math.Min(Width / virtualWidth, Height / virtualHeight);
        if (scale <= 0) return;
        g.TranslateTransform((Width - virtualWidth * scale) / 2f, (Height - virtualHeight * scale) / 2f);
        g.ScaleTransform(scale, scale);
        Color cpu = Color.FromArgb(248, 181, 91);
        Color ram = Color.FromArgb(121, 185, 245);
        using var cpuPen = new Pen(cpu, 2.2f);
        using var ramPen = new Pen(ram, 2.2f);
        using var cpuBrush = new SolidBrush(cpu);
        using var ramBrush = new SolidBrush(ram);
        using var infoBrush = new SolidBrush(ForeColor);
        using var titleFont = new Font("Segoe UI", 13f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var memoryNumberFont = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel);
        void Chip(float x, float y)
        {
            g.DrawRectangle(cpuPen, x + 4, y + 4, 18, 18);
            for (int k = 0; k < 3; k++)
            {
                float q = 8 + k * 5;
                g.DrawLine(cpuPen, x + q, y, x + q, y + 4);
                g.DrawLine(cpuPen, x + q, y + 22, x + q, y + 26);
                g.DrawLine(cpuPen, x, y + q, x + 4, y + q);
                g.DrawLine(cpuPen, x + 22, y + q, x + 26, y + q);
            }
        }
        void Memory(float x, float y)
        {
            g.DrawRectangle(ramPen, x, y + 4, 26, 16);
            for (int k = 0; k < 4; k++)
                g.DrawLine(ramPen, x + 4 + 6 * k, y + 20, x + 4 + 6 * k, y + 24);
            string capacity = Math.Round(totalGiB).ToString("0");
            using var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(capacity, memoryNumberFont, ramBrush, new RectangleF(x + 1, y + 4, 24, 16), center);
        }
        if (twoAcross)
        {
            Chip(4, 5);
            g.DrawString($"{cpuPercent:0}%", titleFont, cpuBrush, 37, 11);
            Memory(79, 5);
            g.DrawString($"{memoryPercent:0}%", titleFont, ramBrush, 112, 11);
        }
        else if (horizontal)
        {
            if (showCpu) { Chip(8, 4); g.DrawString($"{cpuPercent:0}%", titleFont, cpuBrush, 47, 9); }
            if (showRam)
            {
                float row = showCpu ? 39 : 4;
                Memory(8, row);
                g.DrawString($"{memoryPercent:0}%", titleFont, ramBrush, 47, row + 5);
            }
        }
        else
        {
            if (showCpu) { Chip(32, 4); g.DrawString($"{cpuPercent:0}%", titleFont, cpuBrush, 18, 38); }
            if (showRam)
            {
                float row = showCpu ? 71 : 4;
                Memory(32, row);
                g.DrawString($"{memoryPercent:0}%", titleFont, ramBrush, 18, row + 30);
            }
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Stop(); timer.Dispose(); infoTip.Dispose(); }
        base.Dispose(disposing);
    }
}
