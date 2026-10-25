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
    bool IsLanguage => source.Name == "LanguageChoice";
    readonly List<Image> flagImages = new();
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
        foreach (var image in flagImages) image.Dispose();
        flagImages.Clear();
        popup.ShowImageMargin = IsLanguage;
        for (int i = 0; i < source.Items.Count; i++)
        {
            int selectedIndex = i;
            var item = new ToolStripMenuItem(source.Items[i]?.ToString() ?? "")
            {
                Checked = i == source.SelectedIndex,
                AutoSize = true,
                Padding = new Padding(12, 7, 22, 7)
            };
            if (IsLanguage && i < 3)
            {
                var flag = DrawFlag(i);
                flagImages.Add(flag);
                item.Image = flag;
                item.ImageScaling = ToolStripItemImageScaling.None;
            }
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
        if (IsLanguage && source.SelectedIndex is >= 0 and < 3)
        {
            using var image = DrawFlag(source.SelectedIndex);
            g.DrawImage(image, 12, (Height - 18) / 2, 27, 18);
        }
        TextRenderer.DrawText(g, source.Text, Font,
            new Rectangle(IsLanguage ? 47 : 12, 2, Width - (IsLanguage ? 80 : 45), Height - 4), ink,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        using var arrow = new Pen(ink, 1.8f);
        int x = Width - 21, y = Height / 2;
        g.DrawLines(arrow, new[] { new Point(x - 5, y - 2),
            new Point(x, y + 3), new Point(x + 5, y - 2) });
    }
    static Bitmap DrawFlag(int country)
    {
        var bmp = new Bitmap(27, 18);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.White);
        if (country == 0) // Chile
        {
            using var red = new SolidBrush(Color.FromArgb(213, 43, 30));
            using var blue = new SolidBrush(Color.FromArgb(0, 57, 166));
            g.FillRectangle(red, 0, 9, 27, 9);
            g.FillRectangle(blue, 0, 0, 9, 9);
            DrawStar(g, Color.White, 4.5f, 4.5f, 3.2f);
        }
        else if (country == 1) // United States
        {
            using var red = new SolidBrush(Color.FromArgb(178, 34, 52));
            using var blue = new SolidBrush(Color.FromArgb(60, 59, 110));
            for (int k = 0; k < 7; k++) g.FillRectangle(red, 0, k * 18f / 6.5f, 27, 18f / 13f);
            g.FillRectangle(blue, 0, 0, 12, 10);
            using var white = new SolidBrush(Color.White);
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 4; col++) g.FillEllipse(white, 1.2f + col * 2.9f, 1.1f + row * 3.1f, 1.2f, 1.2f);
        }
        else // Brazil
        {
            g.Clear(Color.FromArgb(0, 156, 59));
            using var yellow = new SolidBrush(Color.FromArgb(255, 223, 0));
            using var blue = new SolidBrush(Color.FromArgb(0, 39, 118));
            g.FillPolygon(yellow, new[] { new Point(13, 2), new Point(25, 9), new Point(13, 16), new Point(2, 9) });
            g.FillEllipse(blue, 8, 5, 11, 8);
        }
        using var border = new Pen(Color.FromArgb(145, 154, 166));
        g.DrawRectangle(border, 0, 0, 26, 17);
        return bmp;
    }
    static void DrawStar(Graphics g, Color color, float cx, float cy, float r)
    {
        var points = new PointF[10];
        for (int i = 0; i < 10; i++)
        {
            double angle = -Math.PI / 2 + i * Math.PI / 5;
            float radius = i % 2 == 0 ? r : r * .42f;
            points[i] = new PointF(cx + radius * (float)Math.Cos(angle), cy + radius * (float)Math.Sin(angle));
        }
        using var brush = new SolidBrush(color);
        g.FillPolygon(brush, points);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { popup.Dispose(); foreach (var image in flagImages) image.Dispose(); }
        base.Dispose(disposing);
    }
}
