using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>
/// Optional, read-only retro LCD clock for the Dock. The display is based on
/// the Windows local clock and never modifies the system time.
/// </summary>
public sealed class DigitalClockControl : Control
{
    readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    public bool HorizontalBar { get; }

    public DigitalClockControl(bool horizontalBar)
    {
        HorizontalBar = horizontalBar;
        DoubleBuffered = true;
        Size = horizontalBar ? new Size(136, 54) : new Size(86, 90);
        timer.Tick += (_, _) => RefreshTime();
        RefreshTime();
        timer.Start();
    }

    void RefreshTime()
    {
        AccessibleName = "Reloj digital";
        AccessibleDescription = DateTime.Now.ToString("hh:mm tt", CultureInfo.InvariantCulture);
        Invalidate();
    }

    static void DrawDigit(Graphics g, char digit, float x, float y, float w, float h)
    {
        int[] masks = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };
        int value = digit >= '0' && digit <= '9' ? masks[digit - '0'] : 0;
        const float t = 2.7f;
        RectangleF[] segments =
        {
            new(x + t, y, w - 2*t, t),
            new(x + w - t, y + t, t, h/2 - t),
            new(x + w - t, y + h/2, t, h/2 - t),
            new(x + t, y + h - t, w - 2*t, t),
            new(x, y + h/2, t, h/2 - t),
            new(x, y + t, t, h/2 - t),
            new(x + t, y + h/2 - t/2, w - 2*t, t)
        };
        using var on = new SolidBrush(Color.FromArgb(29, 35, 30));
        using var off = new SolidBrush(Color.FromArgb(28, 62, 67, 56));
        for (int i = 0; i < 7; i++)
            g.FillRectangle((value & (1 << i)) != 0 ? on : off, segments[i]);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        float vw = HorizontalBar ? 126 : 90;
        float vh = HorizontalBar ? 44 : 76;
        float scale = Math.Min(Width / vw, Height / vh);
        if (scale <= 0) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform((Width - vw * scale) / 2, (Height - vh * scale) / 2);
        g.ScaleTransform(scale, scale);
        using var border = new Pen(Color.FromArgb(72, 79, 73), 1.6f);
        using var lcd = new SolidBrush(Color.FromArgb(180, 187, 169));
        var frame = new RectangleF(1.5f, 3, vw - 3, vh - 6);
        g.FillRectangle(lcd, frame);
        g.DrawRectangle(border, frame.X, frame.Y, frame.Width, frame.Height);

        DateTime now = DateTime.Now;
        string time = now.ToString("hhmm", CultureInfo.InvariantCulture);
        string ampm = now.ToString("tt", CultureInfo.InvariantCulture);
        float y = HorizontalBar ? 10 : 15;
        float w = HorizontalBar ? 17 : 16;
        float h = 24;
        float[] positions = HorizontalBar ? new float[] { 8, 27, 56, 75 }
                                          : new float[] { 4, 22, 50, 68 };
        for (int i = 0; i < 4; i++) DrawDigit(g, time[i], positions[i], y, w, h);
        using var ink = new SolidBrush(Color.FromArgb(29, 35, 30));
        float dotX = HorizontalBar ? 49 : 43;
        g.FillEllipse(ink, dotX, y + 7, 3, 3);
        g.FillEllipse(ink, dotX, y + 17, 3, 3);
        using var suffixFont = new Font("Segoe UI", HorizontalBar ? 10f : 11f,
            FontStyle.Bold, GraphicsUnit.Pixel);
        if (HorizontalBar)
            g.DrawString(ampm, suffixFont, ink, 99, 22);
        else
            g.DrawString(ampm, suffixFont, ink, 33, 47);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Stop();
            timer.Dispose();
        }
        base.Dispose(disposing);
    }
}
