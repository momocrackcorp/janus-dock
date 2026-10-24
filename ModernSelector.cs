using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace JanusDock;

/// <summary>Fully custom selector and popup; preserves ComboBox selection as the data model.</summary>
internal sealed class ModernSelector : UserControl
{
    readonly ComboBox source;
    readonly ContextMenuStrip popup;
    bool dark;
    public ModernSelector(ComboBox combo)
    {
        source = combo;
        dark = IsWindowsDark();
        Font = combo.Font;
        Width = Math.Max(combo.Width, TextRenderer.MeasureText(
            LongestItem(combo), Font).Width + 55);
        Height = 39;
        MinimumSize = new Size(130, 39);
        Margin = combo.Margin;
        TabStop = true;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        // Keep the original ComboBox as a non-visible data model so existing
        // event handlers and Apply/Save continue working without modification.
        source.Parent = this;
        source.Visible = false;
        source.TabStop = false;
        source.SelectedIndexChanged += (_, _) => Invalidate();
        source.TextChanged += (_, _) => Invalidate();
        popup = new ContextMenuStrip
        {
            Renderer = new DockMenuRenderer(dark, Color.FromArgb(18, 165, 185)),
            Font = Font,
            ShowImageMargin = false,
            Padding = new Padding(5)
        };
        Click += (_, _) => OpenPopup();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Down)
            {
                OpenPopup();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up && source.SelectedIndex > 0)
                source.SelectedIndex--;
        };
    }
    static string LongestItem(ComboBox cb)
    {
        string best = cb.Text;
        foreach (var item in cb.Items)
            if ((item?.ToString()?.Length ?? 0) > best.Length) best = item?.ToString() ?? best;
        return best;
    }
    static bool IsWindowsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 1)) == 0;
        }
        catch { return false; }
    }
    void OpenPopup()
    {
        dark = IsWindowsDark();
        popup.Renderer = new DockMenuRenderer(dark, Color.FromArgb(18, 165, 185));
        popup.Items.Clear();
        for (int i = 0; i < source.Items.Count; i++)
        {
            int selectedIndex = i;
            var item = new ToolStripMenuItem(source.Items[i]?.ToString() ?? "")
            {
                Checked = i == source.SelectedIndex,
                AutoSize = true,
                Padding = new Padding(12, 7, 22, 7)
            };
            item.Click += (_, _) => { source.SelectedIndex = selectedIndex; Invalidate(); };
            popup.Items.Add(item);
        }
        if (popup.Items.Count > 0) popup.Show(this, new Point(0, Height));
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        dark = IsWindowsDark();
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Color bg = dark ? Color.FromArgb(43, 48, 56) : Color.FromArgb(244, 247, 250);
        Color ink = dark ? Color.WhiteSmoke : Color.FromArgb(30, 43, 56);
        Color border = dark ? Color.FromArgb(85, 102, 115) : Color.FromArgb(197, 207, 216);
        using (var fill = new SolidBrush(bg)) g.FillRectangle(fill, ClientRectangle);
        using (var pen = new Pen(Focused ? Color.FromArgb(18, 165, 185) : border))
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        using (var underline = new Pen(Color.FromArgb(18, 165, 185), 2f))
            g.DrawLine(underline, 1, Height - 2, Width - 2, Height - 2);
        TextRenderer.DrawText(g, source.Text, Font,
            new Rectangle(12, 2, Width - 45, Height - 4), ink,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        using var arrow = new Pen(ink, 1.8f);
        int x = Width - 21, y = Height / 2;
        g.DrawLines(arrow, new[] { new Point(x - 5, y - 2),
            new Point(x, y + 3), new Point(x + 5, y - 2) });
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) popup.Dispose();
        base.Dispose(disposing);
    }
}
