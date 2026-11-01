using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>
/// Compact retro digital watch inset. DD-MM lives inside the top row of the
/// LCD (like the user's F-91W reference) and never overlaps the wordmark.
/// The 244 x 104 virtual layout scales with the configured Dock height.
/// </summary>
public sealed class DigitalClockControl : Control
{
    readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    static readonly Image? CasioWordmark = LoadWordmark();
    public bool HorizontalBar { get; }
    public bool TextOnly { get; set; }
    public bool ShowDate { get; set; } = true;
    public bool ShowSticker { get; set; } = true;
    public bool Hour24 { get; set; }
    public bool ShowAmPm { get; set; } = true;

    public DigitalClockControl(bool horizontalBar)
    {
        HorizontalBar = horizontalBar;
        DoubleBuffered = true;
        Size = horizontalBar ? new Size(122, 52) : new Size(86, 126);
        timer.Tick += (_, _) => RefreshTime();
        RefreshTime();
        timer.Start();
    }

    static Image? LoadWordmark()
    {
        try
        {
            using var stream = typeof(DigitalClockControl).Assembly
                .GetManifestResourceStream("JanusDock.CasioWhite.png");
            if (stream == null) return null;
            using var loaded = Image.FromStream(stream);
            var aged = new Bitmap(loaded);
            // Bone-white silk-screen paint, faded and subtly worn.
            // Preserve transparency; no opaque rectangle behind the decal.
            for (int y = 0; y < aged.Height; y++)
                for (int x = 0; x < aged.Width; x++)
                {
                    Color pixel = aged.GetPixel(x, y);
                    if (pixel.A == 0) continue;
                    int grain = (x * 37 + y * 19 + x * y * 7) % 67;
                    double opacity = grain < 3 ? .25 : grain < 12 ? .53 : .72;
                    int alpha = Math.Clamp((int)Math.Round(pixel.A * opacity), 0, 255);
                    aged.SetPixel(x, y, Color.FromArgb(alpha, 231, 225, 208));
                }
            return aged;
        }
        catch { return null; }
    }

    void RefreshTime()
    {
        AccessibleName = "Reloj digital";
        AccessibleDescription = DateTime.Now.ToString(Hour24 ? "HH:mm" : ShowAmPm ? "hh:mm tt" : "hh:mm", CultureInfo.InvariantCulture)
            + (ShowDate ? DateTime.Now.ToString(", dd-MM", CultureInfo.InvariantCulture) : "");
        Invalidate();
    }

    static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    static void DrawDigit(Graphics g, char digit, float x, float y, float w, float h, bool engraved = false)
    {
        int[] masks = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };
        int value = digit is >= '0' and <= '9' ? masks[digit - '0'] : 0;
        float t = Math.Max(2.6f, w * .12f);
        RectangleF[] segments =
        {
            new(x + t, y, w - t*2, t),
            new(x + w - t, y + t, t, h/2 - t),
            new(x + w - t, y + h/2, t, h/2 - t),
            new(x + t, y + h - t, w - t*2, t),
            new(x, y + h/2, t, h/2 - t),
            new(x, y + t, t, h/2 - t),
            new(x + t, y + h/2 - t/2, w - t*2, t)
        };
        using var ink = new SolidBrush(engraved
            ? Color.FromArgb(229, 231, 230) : Color.FromArgb(24, 29, 25));
        using var ghost = new SolidBrush(engraved
            ? Color.FromArgb(15, 18, 18) : Color.FromArgb(23, 65, 67, 58));
        for (int i = 0; i < segments.Length; i++)
            g.FillRectangle((value & (1 << i)) != 0 ? ink : ghost, segments[i]);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < 2 || Height < 2) return;
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (!HorizontalBar || TextOnly)
        {
            // Plain clock mode has no LCD frame, black panel or CASIO decal.
            g.Clear(BackColor);
            DateTime nowText = DateTime.Now;
            string time = nowText.ToString(Hour24 ? "HH:mm" : "hh:mm", CultureInfo.InvariantCulture);
            string suffixText = !Hour24 && ShowAmPm
                ? nowText.ToString("tt", CultureInfo.InvariantCulture) : "";
            Color inkText = DockContrast.Foreground(BackColor);
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            int clockHeight = HorizontalBar
                ? ShowDate ? Math.Max(21, Height * 2 / 3) : Height
                : 29;
            float size = Math.Clamp(Width / (HorizontalBar ? 7.0f : 5.6f), 11f, 19f);
            using var face = new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Pixel);
            TextRenderer.DrawText(g, time + (HorizontalBar && suffixText.Length > 0 ? " " + suffixText : ""),
                face, new Rectangle(0, HorizontalBar ? 0 : 2, Width, clockHeight),
                inkText, flags);
            int dateY = HorizontalBar ? clockHeight : 35;
            if (!HorizontalBar && suffixText.Length > 0)
            {
                using var suffixFace = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Pixel);
                TextRenderer.DrawText(g, suffixText, suffixFace,
                    new Rectangle(0, 29, Width, 16), inkText, flags);
                dateY = 47;
            }
            if (ShowDate)
            {
                using var dateFace = new Font("Consolas", 11f,
                    FontStyle.Regular, GraphicsUnit.Pixel);
                TextRenderer.DrawText(g,
                    nowText.ToString("dd-MM", CultureInfo.InvariantCulture),
                    dateFace, new Rectangle(0, dateY, Width, 18),
                    inkText, flags);
            }
            return;
        }
        g.Clear(Color.FromArgb(9, 10, 11));

        // Virtual coordinates: 244 x 104 for horizontal capsule.
        // Vertical orientation uses a scaled version of the same control.
        const float vw = 244f, vh = 104f;
        float scale = Math.Min(Width / vw, Height / vh);
        if (scale <= 0) return;
        g.TranslateTransform((Width - vw * scale) / 2f, (Height - vh * scale) / 2f);
        g.ScaleTransform(scale, scale);
        using var divider = new Pen(Color.FromArgb(154, 158, 158), 2.1f);
        g.DrawLine(divider, 3.5f, 18, 3.5f, 86);

        // F-91W style: nested muted watch bezel, a broad gray-green LCD,
        // date at the top right, and a larger time on the lower row. All
        // content stays inside the existing clock control and Dock height.
        var outerBezel = new RectangleF(13.5f, 4.5f, 221f, 73f);
        using (var path = RoundedRectangle(outerBezel, 18f))
        using (var bezelFill = new SolidBrush(Color.FromArgb(24, 29, 33)))
        using (var blueRim = new Pen(Color.FromArgb(48, 91, 131), 2.3f))
        {
            g.FillPath(bezelFill, path);
            g.DrawPath(blueRim, path);
        }
        var redBorder = new RectangleF(17f, 8f, 214f, 67f);
        using (var path = RoundedRectangle(redBorder, 16f))
        using (var redRim = new Pen(Color.FromArgb(120, 55, 58), 1.6f))
            g.DrawPath(redRim, path);
        var shadow = new RectangleF(19.5f, 10.5f, 209f, 62.5f);
        using (var shadowPath = RoundedRectangle(shadow, 14f))
        using (var darkRim = new Pen(Color.FromArgb(76, 84, 81), 4.3f))
            g.DrawPath(darkRim, shadowPath);

        var lcdRect = new RectangleF(22f, 13f, 204f, 57f);
        using (var lcdShape = RoundedRectangle(lcdRect, 12.5f))
        using (var lcdFill = new LinearGradientBrush(lcdRect,
                   Color.FromArgb(199, 207, 193), Color.FromArgb(174, 185, 170),
                   LinearGradientMode.Vertical))
        using (var frame = new Pen(Color.FromArgb(103, 111, 106), 1.35f))
        {
            g.FillPath(lcdFill, lcdShape);
            g.DrawPath(frame, lcdShape);
        }

        DateTime now = DateTime.Now;
        string digits = now.ToString(Hour24 ? "HHmm" : "hhmm", CultureInfo.InvariantCulture);
        bool showPeriod = !Hour24 && ShowAmPm;
        bool isAm = now.Hour < 12;

        // Lower row: four clear seven-segment digits and a colon.
        // In 12-hour mode, AM and PM stay visible with the active one highlighted.
        float[] digitX = { 37f, 67f, 119f, 149f };
        for (int i = 0; i < 4; i++)
            DrawDigit(g, digits[i], digitX[i], 36f, 26f, 31f);
        using var ink = new SolidBrush(Color.FromArgb(24, 29, 25));
        g.FillEllipse(ink, 106.5f, 46f, 4f, 4f);
        g.FillEllipse(ink, 106.5f, 56f, 4f, 4f);
        if (showPeriod)
        {
            // Fixed AM/PM indicators inspired by the approved preview:
            // the active period is ivory on a dark rounded capsule; the
            // inactive one remains a subtle LCD ghost. Never show both as
            // active, and hide both in 24-hour mode or if disabled.
            using var periodFont = new Font("Segoe UI", 12.5f,
                FontStyle.Bold, GraphicsUnit.Pixel);
            using var activeFill = new SolidBrush(Color.FromArgb(27, 34, 30));
            using var activeText = new SolidBrush(Color.FromArgb(231, 237, 219));
            using var inactiveText = new SolidBrush(Color.FromArgb(105, 115, 104));
            using var centered = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            };
            var amArea = new RectangleF(179.5f, 46f, 22f, 19f);
            var pmArea = new RectangleF(202f, 46f, 22f, 19f);
            using (var selected = RoundedRectangle(isAm ? amArea : pmArea, 4.7f))
                g.FillPath(activeFill, selected);
            g.DrawString("AM", periodFont, isAm ? activeText : inactiveText,
                amArea, centered);
            g.DrawString("PM", periodFont, isAm ? inactiveText : activeText,
                pmArea, centered);
        }

        if (ShowDate)
        {
            // TOP RIGHT LCD: DD-MM, replacing the reference's weekday/day.
            // Keeping date INSIDE the display leaves CASIO fully unobstructed.
            string date = now.ToString("dd-MM", CultureInfo.InvariantCulture);
            float[] dateX = { 132f, 150f, 184f, 202f };
            int[] dateCharIndex = { 0, 1, 3, 4 };
            for (int i = 0; i < 4; i++)
                DrawDigit(g, date[dateCharIndex[i]], dateX[i], 17f, 14f, 16f);
            g.FillRectangle(ink, 169f, 25f, 11f, 2.7f);
        }
        if (ShowSticker)
        {
            if (CasioWordmark != null)
            {
                var old = g.InterpolationMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                // 10% smaller than the prior wordmark, centered beneath
                // the LCD, with an extra ~2 visible pixels of breathing room
                // above the wordmark (without clipping the Dock bottom).
                const float logoWidth = 92.54f;
                const float lcdCenterX = 124f;
                g.DrawImage(CasioWordmark, new RectangleF(
                    lcdCenterX - logoWidth / 2f, 83f, logoWidth, 16.59f));
                g.InterpolationMode = old;
            }
            else
            {
                // Text fallback matches the centered, smaller decal.
                using var brandFont = new Font("Arial Black", 16.6f,
                    FontStyle.Bold, GraphicsUnit.Pixel);
                using var white = new SolidBrush(Color.FromArgb(174, 231, 225, 208));
                using var centered = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString("CASIO", brandFont, white,
                    new RectangleF(77.7f, 81f, 92.6f, 19f), centered);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Stop();
            timer.Dispose();
        }
        base.Dispose(disposing);
    }
}
