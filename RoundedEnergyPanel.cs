using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JanusDock;

// Capsule behind the Dock's power actions; keeps standard FlowLayoutPanel behavior.
public sealed class RoundedEnergyPanel : FlowLayoutPanel
{
    public Color CapsuleColor { get; set; } = Color.FromArgb(56, 76, 86);
    public Color CapsuleBorder { get; set; } = Color.FromArgb(80, 210, 224, 231);

    public RoundedEnergyPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var background = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(background, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < 12 || Height < 12) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(2, 3, Width - 5, Height - 7);
        int radius = Math.Min(22, Math.Min(rect.Width, rect.Height) / 2);
        int d = radius * 2;
        using var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        using var fill = new SolidBrush(CapsuleColor);
        using var border = new Pen(CapsuleBorder, 1f);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }
}
