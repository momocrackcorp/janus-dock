using System;
using System.Drawing;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>Custom face for an existing ComboBox, retaining its native keyboard-accessible dropdown.</summary>
internal sealed class ModernSelector : UserControl
{
    readonly ComboBox combo;
    readonly Panel face;
    public ModernSelector(ComboBox source)
    {
        combo = source;
        Width = source.Width;
        Height = Math.Max(35, source.Height + 5);
        MinimumSize = new Size(80, 35);
        Margin = source.Margin;
        BackColor = Color.FromArgb(241, 245, 248);
        combo.Parent = this;
        combo.Location = new Point(0, 0);
        combo.Width = Width;
        combo.Visible = true;
        combo.TabStop = true;
        face = new Panel { Dock = DockStyle.Fill, BackColor = BackColor, Cursor = Cursors.Hand, TabStop = true };
        Controls.Add(face);
        face.BringToFront();
        face.Paint += (_, e) => DrawFace(e.Graphics);
        face.MouseClick += (_, _) => OpenSelector();
        face.KeyDown += (_, e) =>
        {
            if (e.KeyCode is Keys.Space or Keys.Enter or Keys.Down) { OpenSelector(); e.Handled = true; }
        };
        combo.SelectedIndexChanged += (_, _) => face.Invalidate();
        combo.TextChanged += (_, _) => face.Invalidate();
        GotFocus += (_, _) => face.Focus();
        Resize += (_, _) => { combo.Width = Width; face.Invalidate(); };
    }
    void OpenSelector()
    {
        combo.Focus();
        combo.DroppedDown = true;
    }
    void DrawFace(Graphics g)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var bounds = ClientRectangle;
        var dark = SystemColors.Window.GetBrightness() < .5f;
        Color bg = dark ? Color.FromArgb(43, 48, 56) : Color.FromArgb(244, 247, 250);
        Color ink = dark ? Color.WhiteSmoke : Color.FromArgb(30, 43, 56);
        Color border = dark ? Color.FromArgb(85, 102, 115) : Color.FromArgb(197, 207, 216);
        using (var brush = new SolidBrush(bg)) g.FillRectangle(brush, bounds);
        using (var pen = new Pen(border)) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        using (var accent = new Pen(Color.FromArgb(18, 165, 185), 2f)) g.DrawLine(accent, 1, Height - 2, Width - 2, Height - 2);
        TextRenderer.DrawText(g, combo.Text, Font, new Rectangle(11, 1, Width - 40, Height - 2),
            ink, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        using var chevron = new Pen(ink, 1.7f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        int cx = Width - 19, cy = Height / 2;
        g.DrawLines(chevron, new[]{ new Point(cx - 5, cy - 2), new Point(cx, cy + 3), new Point(cx + 5, cy - 2) });
    }
}
