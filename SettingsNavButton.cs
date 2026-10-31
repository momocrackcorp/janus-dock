using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JanusDock;

// Sidebar entry matching the approved Windows 11-style Settings concept.
internal sealed class SettingsNavButton : Button
{
    public bool SelectedPage { get; set; }

    public SettingsNavButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var surface = Parent?.BackColor ?? Color.FromArgb(235, 243, 251);
        e.Graphics.Clear(surface);
        if (SelectedPage || ClientRectangle.Contains(PointToClient(Cursor.Position)))
        {
            var rect = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            using var path = new GraphicsPath();
            const int d = 16;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            using var fill = new SolidBrush(SelectedPage
                ? Color.FromArgb(208, 232, 252)
                : Color.FromArgb(225, 237, 249));
            e.Graphics.FillPath(fill, path);
            if (SelectedPage)
            {
                using var marker = new SolidBrush(Color.FromArgb(0, 115, 213));
                e.Graphics.FillRectangle(marker, 2, 11, 4, Height - 22);
            }
        }
        TextRenderer.DrawText(e.Graphics, Text, Font,
            new Rectangle(17, 0, Math.Max(1, Width - 24), Height),
            SelectedPage ? Color.FromArgb(0, 104, 189) : Color.FromArgb(35, 53, 74),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
    }

    protected override void OnMouseEnter(System.EventArgs e)
    {
        base.OnMouseEnter(e);
        Invalidate();
    }

    protected override void OnMouseLeave(System.EventArgs e)
    {
        base.OnMouseLeave(e);
        Invalidate();
    }
}
