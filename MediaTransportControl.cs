using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>
/// Simple OS media-key transport (not a Spotify API client).
/// Works with audio players that accept Windows media keys.
/// No remote sessions, authentication, metadata or personal data.
/// </summary>
public sealed class MediaTransportControl : Control
{
    private const byte MediaPrevious = 0xB1;
    private const byte MediaPlayPause = 0xB3;
    private const byte MediaNext = 0xB0;
    private const uint KeyUp = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    readonly bool horizontal;
    readonly ToolTip tips = new();
    public MediaTransportControl(bool horizontalBar)
    {
        horizontal = horizontalBar;
        DoubleBuffered = true;
        SetStyle(ControlStyles.Selectable, true);
        Cursor = Cursors.Hand;
        AccessibleName = "Control multimedia de Windows";
        Size = horizontal ? new Size(108, 42) : new Size(78, 118);
        tips.SetToolTip(this, "Anterior · Reproducir/Pausar · Siguiente");
    }

    private static void Key(byte vk)
    {
        // Send only the OS media command; never steal window focus.
        keybd_event(vk, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, KeyUp, UIntPtr.Zero);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;
        int cell = horizontal ? Math.Max(1, Width / 3) : Math.Max(1, Height / 3);
        int index = Math.Clamp((horizontal ? e.X : e.Y) / cell, 0, 2);
        Key(index switch { 0 => MediaPrevious, 1 => MediaPlayPause, _ => MediaNext });
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        var ink = BackColor.GetBrightness() > .55f
            ? Color.FromArgb(34, 53, 76) : Color.FromArgb(205, 218, 233);
        using var pen = new Pen(ink, 2.1f)
        {
            StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round
        };
        using var brush = new SolidBrush(ink);
        using var boundary = new Pen(Color.FromArgb(75, 98, 120), 1);
        int gap = 3;
        for (int i = 0; i < 3; i++)
        {
            var rect = horizontal
                ? new Rectangle(i * Width / 3 + gap, gap, Width / 3 - 2 * gap, Height - 2 * gap)
                : new Rectangle(gap, i * Height / 3 + gap, Width - 2 * gap, Height / 3 - 2 * gap);
            if (rect.Width < 10 || rect.Height < 10) continue;
            using var rounded = new GraphicsPath();
            int radius = Math.Min(9, Math.Min(rect.Width, rect.Height) / 3);
            rounded.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            rounded.AddArc(rect.Right - 2 * radius, rect.Y, radius * 2, radius * 2, 270, 90);
            rounded.AddArc(rect.Right - 2 * radius, rect.Bottom - 2 * radius, radius * 2, radius * 2, 0, 90);
            rounded.AddArc(rect.X, rect.Bottom - 2 * radius, radius * 2, radius * 2, 90, 90);
            rounded.CloseFigure();
            g.DrawPath(boundary, rounded);
            float cx = rect.X + rect.Width / 2f, cy = rect.Y + rect.Height / 2f;
            if (i == 1)
            {
                // Lucide play icon for the play/pause toggle.
                g.DrawPolygon(pen, new PointF[]
                {
                    new(cx - 4, cy - 7), new(cx + 7, cy),
                    new(cx - 4, cy + 7)
                });
            }
            else
            {
                bool next = i == 2;
                float side = next ? 1 : -1;
                g.DrawLine(pen, cx + 7 * side, cy - 7, cx + 7 * side, cy + 7);
                g.DrawPolygon(pen, new PointF[]
                {
                    new(cx + 4 * side, cy - 7), new(cx - 6 * side, cy),
                    new(cx + 4 * side, cy + 7)
                });
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) tips.Dispose();
        base.Dispose(disposing);
    }
}
