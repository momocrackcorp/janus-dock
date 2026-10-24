using System.Drawing;
using System.Windows.Forms;

namespace JanusDock;

// Custom WinForms drawing, including submenu surfaces and selection.
// Uses the Windows light/dark preference and accent rather than legacy System rendering.
internal sealed class DockMenuRenderer : ToolStripProfessionalRenderer
{
    readonly Color bg, fg, hover, border, accent;
    public DockMenuRenderer(bool dark, Color accentColor) : base(new DockMenuColors(dark, accentColor))
    {
        bg = dark ? Color.FromArgb(35, 39, 47) : Color.FromArgb(250, 251, 253);
        fg = dark ? Color.FromArgb(240, 242, 246) : Color.FromArgb(35, 40, 48);
        hover = dark ? Color.FromArgb(56, 69, 82) : Color.FromArgb(224, 238, 249);
        border = dark ? Color.FromArgb(79, 88, 101) : Color.FromArgb(211, 218, 226);
        accent = accentColor;
        RoundedEdges = false;
    }
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(bg);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(border);
        var r = e.ToolStrip.ClientRectangle;
        if (r.Width > 1 && r.Height > 1) e.Graphics.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
    }
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(bg);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled) return;
        using var brush = new SolidBrush(hover);
        var r = new Rectangle(3, 2, e.Item.Width - 6, e.Item.Height - 4);
        e.Graphics.FillRectangle(brush, r);
        using var pen = new Pen(accent, 2);
        e.Graphics.DrawLine(pen, 3, 4, 3, e.Item.Height - 4);
    }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? fg : Color.Gray;
        base.OnRenderItemText(e);
    }
    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(border);
        int y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
    }
    sealed class DockMenuColors : ProfessionalColorTable
    {
        readonly Color bg, hover;
        public DockMenuColors(bool dark, Color accent)
        {
            bg = dark ? Color.FromArgb(35, 39, 47) : Color.FromArgb(250, 251, 253);
            hover = dark ? Color.FromArgb(56, 69, 82) : Color.FromArgb(224, 238, 249);
            UseSystemColors = false;
        }
        public override Color ToolStripDropDownBackground => bg;
        public override Color MenuBorder => bg;
        public override Color ImageMarginGradientBegin => bg;
        public override Color ImageMarginGradientMiddle => bg;
        public override Color ImageMarginGradientEnd => bg;
        public override Color MenuItemSelected => hover;
        public override Color MenuItemSelectedGradientBegin => hover;
        public override Color MenuItemSelectedGradientEnd => hover;
    }
}
internal static class DockMenuStyle
{
    public static ContextMenuStrip Create(bool light, Color accent)
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new DockMenuRenderer(!light, accent),
            Font = new Font("Segoe UI", 10f),
            Padding = new Padding(5),
            ShowImageMargin = false
        };
        return menu;
    }
}
