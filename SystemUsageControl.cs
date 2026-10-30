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
        // Draw inside a normalized coordinate system, preserving icon proportions
        // even when Windows display scaling or dock thickness changes.
        bool horizontal = Horizontal;
        float virtualWidth = horizontal ? 250f : 90f;
        float virtualHeight = horizontal ? 70f : 150f;
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
        if (horizontal)
        {
            // Two dedicated rows prevent CPU and RAM values overlapping.
            Chip(8, 4);
            g.DrawString($"CPU {cpuPercent:0}%", titleFont, cpuBrush, 47, 9);
            Memory(8, 39);
            g.DrawString($"RAM {memoryPercent:0}%", titleFont, ramBrush, 47, 44);

        }
        else
        {
            Chip(32, 4);
            g.DrawString($"CPU {cpuPercent:0}%", titleFont, cpuBrush, 13, 38);
            Memory(32, 71);
            g.DrawString($"RAM {memoryPercent:0}%", titleFont, ramBrush, 12, 101);

        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Stop(); timer.Dispose(); }
        base.Dispose(disposing);
    }
}
