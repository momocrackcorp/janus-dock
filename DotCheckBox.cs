using System;
using System.Drawing;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>Independent checkbox drawn as a round dot, not a mutually exclusive radio button.</summary>
internal sealed class DotCheckBox : CheckBox
{
    public DotCheckBox()
    {
        AutoSize = true;
        FlatStyle = FlatStyle.Flat;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var textSize = TextRenderer.MeasureText(Text, Font, new Size(10000, 10000),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        return new Size(textSize.Width + 31, Math.Max(27, textSize.Height + 8));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Color bg = Parent?.BackColor ?? SystemColors.Control;
        Color fg = Enabled ? ForeColor : SystemColors.GrayText;
        Color accent = Color.FromArgb(19, 164, 183);
        using (var brush = new SolidBrush(bg)) g.FillRectangle(brush, ClientRectangle);
        int diameter = 18;
        int y = (Height - diameter) / 2;
        using (var pen = new Pen(Checked ? accent : fg, 1.7f))
            g.DrawEllipse(pen, 2, y, diameter, diameter);
        if (Checked)
        {
            using var dot = new SolidBrush(accent);
            g.FillEllipse(dot, 7, y + 5, diameter - 10, diameter - 10);
        }
        TextRenderer.DrawText(g, Text, Font, new Rectangle(28, 0, Math.Max(0, Width - 28), Height),
            fg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(g, new Rectangle(26, 2, Math.Max(1, Width - 28), Height - 4));
    }
}
