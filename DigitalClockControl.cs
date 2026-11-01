using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>
/// LCD inset, engraved day-month and the user's actual transparent white logo.
/// The horizontal layout is based on the approved 500 x 120 reference crop;
/// its virtual drawing coordinates scale to the configured dock thickness.
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

        // Light gray-green LCD embedded in the same black housing.
        // Compact inset: keep a black border above and below the display.
        var shadow = new RectangleF(19, 19, 213, 44);
        using (var shadowShape = RoundedRectangle(shadow, 15))
        using (var darkBorder = new Pen(Color.FromArgb(40, 44, 42), 4.1f))
        {
            g.DrawPath(darkBorder, shadowShape);
        }
        var lcdRect = new RectangleF(20.5f, 20.5f, 210, 41);
        using (var lcdShape = RoundedRectangle(lcdRect, 14))
        using (var lcdFill = new LinearGradientBrush(lcdRect,
                   Color.FromArgb(200, 206, 191), Color.FromArgb(174, 183, 167),
                   LinearGradientMode.Vertical))
        using (var frame = new Pen(Color.FromArgb(98, 106, 103), 1.5f))
        {
            g.FillPath(lcdFill, lcdShape);
            g.DrawPath(frame, lcdShape);
        }

        DateTime now = DateTime.Now;
        string digits = now.ToString(Hour24 ? "HHmm" : "hhmm", CultureInfo.InvariantCulture);
        string suffix = !Hour24 && ShowAmPm ? now.ToString("tt", CultureInfo.InvariantCulture) : "";
        float[] digitX = { 52, 81, 122, 151 };
        for (int i = 0; i < 4; i++) DrawDigit(g, digits[i], digitX[i], 28, 24, 25);
        using var ink = new SolidBrush(Color.FromArgb(24, 29, 25));
        g.FillEllipse(ink, 111, 34, 3.4f, 3.4f);
        g.FillEllipse(ink, 111, 45, 3.4f, 3.4f);
        if (suffix.Length > 0)
            using (var suffixFont = new Font("Segoe UI", 14, FontStyle.Bold, GraphicsUnit.Pixel))
                g.DrawString(suffix, suffixFont, ink, 185, 38);

        if (ShowDate)
        {
            // The white engraved date uses the same seven-segment design as
            // the approved screenshot, not a separate pill or text label.
            string date = now.ToString("dd-MM", CultureInfo.InvariantCulture);
            float[] x = { 33f, 49f, 73f, 89f };
            int[] k = { 0, 1, 3, 4 };
            for (int i = 0; i < 4; i++)
                DrawDigit(g, date[k[i]], x[i], 74, 12, 19, engraved: true);
            using var dash = new SolidBrush(Color.FromArgb(226, 229, 228));
            g.FillRectangle(dash, 63, 82, 8, 2.5f);
        }
        if (ShowSticker)
        {
            if (CasioWordmark != null)
            {
                var old = g.InterpolationMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                // 3% smaller, shifted right 2 virtual pixels: keep the C
                // clear of the engraved date without changing the round size.
                g.DrawImage(CasioWordmark, new RectangleF(126f, 78.3f, 102.82f, 18.43f));
                g.InterpolationMode = old;
            }
            else
            {
                using var brandFont = new Font("Arial Black", 18.43f, FontStyle.Bold, GraphicsUnit.Pixel);
                using var white = new SolidBrush(Color.FromArgb(174, 231, 225, 208));
                g.DrawString("CASIO", brandFont, white, 126, 75.3f);
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
