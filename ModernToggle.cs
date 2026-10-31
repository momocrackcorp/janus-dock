using System;
using System.Drawing;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>Windows 11-inspired toggle switch bound to an existing CheckBox.</summary>
internal sealed class ModernToggle : UserControl
{
    readonly CheckBox model;
    public bool RightAligned { get; set; }
    public ModernToggle(CheckBox original)
    {
        model = original;
        Font = original.Font;
        Height = 34;
        Width = Math.Max(320, TextRenderer.MeasureText(original.Text, Font).Width + 78);
        Margin = original.Margin;
        Cursor = Cursors.Hand;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        model.Parent = this;
        model.Visible = false;
        model.TabStop = false;
        model.CheckedChanged += (_, _) => Invalidate();
        // An option disabled by another setting remains non-interactive in
        // the custom switch as well (the hidden CheckBox is the authority).
        model.EnabledChanged += (_, _) => Invalidate();
        Click += (_, _) => { if (model.Enabled) model.Checked = !model.Checked; };
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            { if (model.Enabled) model.Checked = !model.Checked; e.Handled = true; }
        };
        model.TextChanged += (_, _) => Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        bool on = model.Checked;
        Color accent = model.Enabled ? Color.FromArgb(18, 165, 185)
                                     : Color.FromArgb(164, 168, 173);
        int x = RightAligned ? Math.Max(5, Width - 52) : 5;
        int y = (Height - 22) / 2;
        using var track = new SolidBrush(on ? accent : Color.FromArgb(155, 163, 174));
        using var knob = new SolidBrush(Color.White);
        using var shape = new System.Drawing.Drawing2D.GraphicsPath();
        shape.AddArc(x, y, 22, 22, 90, 180);
        shape.AddArc(x + 22, y, 22, 22, 270, 180);
        shape.CloseFigure();
        g.FillPath(track, shape);
        g.FillEllipse(knob, on ? x + 25 : x + 3, y + 3, 16, 16);
        var labelBounds = RightAligned
            ? new Rectangle(10, 0, Math.Max(20, Width - 80), Height)
            : new Rectangle(59, 0, Math.Max(20, Width - 63), Height);
        TextRenderer.DrawText(g, model.Text, Font, labelBounds,
            model.Enabled ? ForeColor : Color.FromArgb(145, 151, 159), TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
}
