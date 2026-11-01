using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JanusDock;

// Full dock-height energy area. In clock mode it becomes one continuous,
// black left-rounded/right-square capsule, exactly like the chosen reference.
public sealed class RoundedEnergyPanel : FlowLayoutPanel
{
    public Color CapsuleColor { get; set; } = Color.FromArgb(56, 76, 86);
    bool horizontalBar = true, integratedClockStyle;
    public bool HorizontalBar
    {
        get => horizontalBar;
        set { if (horizontalBar != value) { horizontalBar = value; Invalidate(); } }
    }
    public bool IntegratedClockStyle
    {
        get => integratedClockStyle;
        set { if (integratedClockStyle != value)
              { integratedClockStyle = value; Invalidate(); } }
    }

    public RoundedEnergyPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        Padding = Padding.Empty;
    }

    private GraphicsPath CapsulePath()
    {
        var path = new GraphicsPath();
        float radius = IntegratedClockStyle ? Height / 2f
            : Math.Min(Height * .43f, 24f);
        float d = radius * 2f;
        // Draw through the outer pixel edge; transparent-looking corners
        // get the *exact* Dock background beneath the anti-aliased arc.
        path.AddArc(0, 0, d, d, 180, 90);
        path.AddLine(radius, 0, Width, 0);
        path.AddLine(Width, 0, Width, Height);
        path.AddLine(Width, Height, radius, Height);
        path.AddArc(0, Height - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Avoid WinForms Region clipping: GDI Regions rasterize arcs to
        // whole pixels and leave light/dotted seams against dark Dock themes.
        // Paint the actual parent background FIRST, then the smooth capsule.
        // No rectangular black surface remains outside the rounded contour.
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        if (Width < 4 || Height < 4) return;
        using var fill = new SolidBrush(CapsuleColor);
        if (!HorizontalBar)
        {
            e.Graphics.FillRectangle(fill, ClientRectangle);
            return;
        }
        using var path = CapsulePath();
        // No Region clipping: the edge is blended over the actual Dock color.
        e.Graphics.FillPath(fill, path);
        // No pale outline: on dark/purple bars the old fixed-gray rim
        // looked like a white halo around the round's anti-aliased edge.
        // Clipping and capsule fill already define the rounded silhouette.
    }
}
