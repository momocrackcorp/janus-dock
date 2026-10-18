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
    ulong lastIdle, lastTotal;
    bool initialized;
    double cpuPercent, memoryPercent, usedGiB, totalGiB;
    public bool Horizontal { get; }
    public SystemUsageControl(bool horizontal)
    {
        Horizontal = horizontal;
        Size = horizontal ? new Size(265, 56) : new Size(88, 150);
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
            Invalidate();
        }
        catch { /* Keep last valid sample. */ }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Color cpu = Color.FromArgb(248, 181, 91), ram = Color.FromArgb(121, 185, 245);
        using var font = new Font("Segoe UI", Horizontal ? 9f : 8.5f, FontStyle.Bold);
        using var small = new Font("Segoe UI", 8f);
        using var cpuPen = new Pen(cpu, 2.2f);
        using var ramPen = new Pen(ram, 2.2f);
        void Chip(int x, int y, Pen pen)
        {
            g.DrawRoundedRectangle(pen, x + 4, y + 4, 19, 19, 3);
            for (int k = 0; k < 3; k++)
            {
                int q = 8 + k * 5;
                g.DrawLine(pen, x + q, y, x + q, y + 4);
                g.DrawLine(pen, x + q, y + 23, x + q, y + 27);
                g.DrawLine(pen, x, y + q, x + 4, y + q);
                g.DrawLine(pen, x + 23, y + q, x + 27, y + q);
            }
        }
        void MemoryIcon(int x, int y)
        {
            g.DrawRoundedRectangle(ramPen, x, y + 5, 28, 17, 3);
            for (int k = 0; k < 4; k++) g.DrawLine(ramPen, x + 4 + 6 * k, y + 22, x + 4 + 6 * k, y + 26);
        }
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        if (Horizontal)
        {
            Chip(5, 13, cpuPen);
            TextRenderer.DrawText(g, $"CPU {cpuPercent:0}%", font, new Point(40, 17), cpu, flags);
            MemoryIcon(120, 13);
            TextRenderer.DrawText(g, $"RAM {memoryPercent:0}%", font, new Point(154, 8), ram, flags);
            TextRenderer.DrawText(g, $"{usedGiB:0.0}/{totalGiB:0.0} GB", small, new Point(154, 29), ForeColor, flags);
        }
        else
        {
            Chip((Width - 27) / 2, 3, cpuPen);
            TextRenderer.DrawText(g, $"CPU {cpuPercent:0}%", font, new Rectangle(0, 35, Width, 25), cpu, TextFormatFlags.HorizontalCenter | flags);
            MemoryIcon((Width - 28) / 2, 66);
            TextRenderer.DrawText(g, $"RAM {memoryPercent:0}%", font, new Rectangle(0, 96, Width, 22), ram, TextFormatFlags.HorizontalCenter | flags);
            TextRenderer.DrawText(g, $"{usedGiB:0.0}/{totalGiB:0.0} GB", small, new Rectangle(0, 120, Width, 28), ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Stop(); timer.Dispose(); }
        base.Dispose(disposing);
    }
}
