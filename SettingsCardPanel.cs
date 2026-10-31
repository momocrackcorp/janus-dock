using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JanusDock;

// Separate card surface for the Settings sidebar pages.
internal sealed class SettingsCardPanel : FlowLayoutPanel
{
    public SettingsCardPanel()
    {
        Name = "SettingsCard";
        DoubleBuffered = true;
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        AutoScroll = false;
        AutoSize = false;
        Padding = new Padding(16, 8, 16, 9);
        Margin = new Padding(5, 0, 5, 8);
        BackColor = Color.FromArgb(252, 254, 255);
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.FromArgb(229, 236, 244));
        if (Width < 18 || Height < 18) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        const int radius = 14;
        using var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, radius * 2, radius * 2, 180, 90);
        path.AddArc(rect.Right - radius * 2, rect.Top, radius * 2, radius * 2, 270, 90);
        path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        using var fill = new SolidBrush(BackColor);
        using var outline = new Pen(Color.FromArgb(185, 201, 220), 1f);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(outline, path);
    }
}
