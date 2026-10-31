using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JanusDock;

// Full dock-height energy area. In clock mode it becomes one continuous,
// black left-rounded/right-square capsule, exactly like the chosen reference.
public sealed class RoundedEnergyPanel : FlowLayoutPanel
{
    public Color CapsuleColor { get; set; } = Color.FromArgb(56, 76, 86);
    public bool HorizontalBar { get; set; } = true;
    public bool IntegratedClockStyle { get; set; }

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
        e.Graphics.Clear(BackColor);
        if (Width < 2 || Height < 2) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(CapsuleColor);
        if (!HorizontalBar)
        {
            e.Graphics.FillRectangle(fill, ClientRectangle);
            return;
        }
        // Only left end is rounded. The right end reaches the physical edge.
        float radius = IntegratedClockStyle ? Math.Max(1f, (Height - 2) / 2f)
                                            : Math.Min(Height * .43f, 24f);
        float diameter = radius * 2;
        using var path = new GraphicsPath();
        path.AddArc(1, 1, diameter, diameter, 180, 90);
        path.AddLine(radius + 1, 1, Width - 1, 1);
        path.AddLine(Width - 1, 1, Width - 1, Height - 2);
        path.AddLine(Width - 1, Height - 2, radius + 1, Height - 2);
        path.AddArc(1, Height - diameter - 2, diameter, diameter, 90, 90);
        path.CloseFigure();
        e.Graphics.FillPath(fill, path);
        if (IntegratedClockStyle)
        {
            // Subtle metal rim, no decorative horizontal rules below the date.
            using var rim = new Pen(Color.FromArgb(133, 141, 141), 1.15f);
            e.Graphics.DrawPath(rim, path);
        }
    }
}
