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
        set { if (horizontalBar == value) return; horizontalBar = value; UpdateShape(); Invalidate(); }
    }
    public bool IntegratedClockStyle
    {
        get => integratedClockStyle;
        set { if (integratedClockStyle == value) return; integratedClockStyle = value; UpdateShape(); Invalidate(); }
    }

    public RoundedEnergyPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        Padding = new Padding(0);
    }

    private GraphicsPath CapsulePath()
    {
        var path = new GraphicsPath();
        float radius = IntegratedClockStyle
            ? (Height - 1f) / 2f : Math.Min(Height * .43f, 24f);
        float d = radius * 2f;
        // A round left end with a precisely square right edge.
        path.AddArc(0, 0, d, d, 180, 90);
        path.AddLine(radius, 0, Width - 1, 0);
        path.AddLine(Width - 1, 0, Width - 1, Height - 1);
        path.AddLine(Width - 1, Height - 1, radius, Height - 1);
        path.AddArc(0, Height - 1 - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void UpdateShape()
    {
        var previous = Region;
        if (!HorizontalBar || Width < 4 || Height < 4)
            Region = null;
        else
        {
            using var path = CapsulePath();
            Region = new Region(path);
        }
        previous?.Dispose();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateShape();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (Width < 4 || Height < 4) return;
        using var fill = new SolidBrush(CapsuleColor);
        if (!HorizontalBar)
        {
            e.Graphics.FillRectangle(fill, ClientRectangle);
            return;
        }
        using var path = CapsulePath();
        // Match WinForms clipping to the curved surface so no rectangular
        // background can leak out near the round's upper-left corner.
        e.Graphics.FillPath(fill, path);
        if (IntegratedClockStyle)
        {
            using var rim = new Pen(Color.FromArgb(133, 141, 141), 1.15f);
            e.Graphics.DrawPath(rim, path);
        }
    }
}
