using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JanusDock;

// A full-height end section, not a floating capsule.
public sealed class RoundedEnergyPanel : FlowLayoutPanel
{
    public Color CapsuleColor { get; set; } = Color.FromArgb(56, 76, 86);
    public bool HorizontalBar { get; set; } = true;

    public RoundedEnergyPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        Padding = new Padding(0);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Paint the parent-facing background first so rounded corners reveal the main dock.
        e.Graphics.Clear(BackColor);
        if (Width < 2 || Height < 2) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(CapsuleColor);
        if (!HorizontalBar)
        {
            // Vertical dock: full-width square footer.
            e.Graphics.FillRectangle(fill, ClientRectangle);
            return;
        }
        // Horizontal dock: round only the left corners, square flush at the screen edge.
        float radius = Math.Min(Height * .43f, 24f);
        float d = radius * 2;
        using var path = new GraphicsPath();
        path.AddArc(0, 0, d, d, 180, 90);
        path.AddLine(radius, 0, Width, 0);
        path.AddLine(Width, 0, Width, Height);
        path.AddLine(Width, Height, radius, Height);
        path.AddArc(0, Height - d, d, d, 90, 90);
        path.CloseFigure();
        e.Graphics.FillPath(fill, path);
    }
}
