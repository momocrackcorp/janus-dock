using System.Drawing;
using System.Drawing.Drawing2D;

namespace JanusDock;

// Thin 2px/24px vector icons following Lucide's familiar outline geometry.
// Only built-in dock commands/folders use these icons; application icons remain native.
internal static class LucideDockIcons
{
    public static Image? Load(string path, bool light)
    {
        string type = path.ToLowerInvariant() switch
        {
            "shell:mycomputerfolder" => "monitor",
            "janus:documents" => "folder",
            "janus:downloads" => "download",
            "janus:settings" => "settings",
            "janus:network" => "network",
            "janus:shutdown" => "power",
            "janus:restart" => "restart",
            _ => ""
        };
        if (type.Length == 0)
        {
            string? app = CommonApps.IconType(path);
            type = app switch
            {
                "msedge.exe" => "browser",
                "chrome.exe" => "browser",
                "brave.exe" => "browser",
                "firefox.exe" => "browser",
                "winword.exe" => "word",
                "excel.exe" => "excel",
                "powerpnt.exe" => "powerpoint",
                "outlook.exe" => "outlook",
                "onenote.exe" => "onenote",
                _ => ""
            };
        }
        if (type.Length == 0)
        {
            if (Directory.Exists(path)) type = "folder";
            else return null;
        }
        var bitmap = new Bitmap(96, 96);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.Transparent);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(0, 0);
        g.ScaleTransform(4, 4);
        // Distinctive pastel outlines, consistent on dark and light docks.
        var color = type switch
        {
            "power" => Color.FromArgb(255, 98, 104),
            "restart" => Color.FromArgb(65, 219, 145),
            "folder" => Color.FromArgb(241, 191, 80),
            "monitor" => Color.FromArgb(159, 196, 241),
            "download" => Color.FromArgb(110, 183, 247),
            "settings" => Color.FromArgb(203, 211, 224),
            "network" => Color.FromArgb(242, 199, 79),
            "browser" => Color.FromArgb(93, 188, 241),
            "word" => Color.FromArgb(66, 145, 232),
            "excel" => Color.FromArgb(48, 190, 126),
            "powerpoint" => Color.FromArgb(239, 142, 77),
            "outlook" => Color.FromArgb(91, 177, 230),
            "onenote" => Color.FromArgb(184, 119, 228),
            _ => light ? Color.FromArgb(83, 98, 119) : Color.FromArgb(187, 197, 212)
        };
        using var pen = new Pen(color, 2) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        void Line(float x, float y, float xx, float yy) => g.DrawLine(pen, x, y, xx, yy);
        switch (type)
        {
            case "browser":
                g.DrawEllipse(pen, 3, 3, 18, 18);
                g.DrawEllipse(pen, 8, 3, 8, 18);
                Line(3, 12, 21, 12);
                break;
            case "word":
            case "excel":
            case "powerpoint":
            case "outlook":
            case "onenote":
                g.DrawRoundedRectangleCompat(pen, 3, 3, 18, 18, 2);
                string letter = type switch { "word" => "W", "excel" => "X", "powerpoint" => "P", "outlook" => "O", _ => "N" };
                using (var font = new Font("Segoe UI", 13, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(color))
                {
                    var textSize = g.MeasureString(letter, font);
                    g.DrawString(letter, font, brush, (24 - textSize.Width) / 2, (24 - textSize.Height) / 2);
                }
                break;
            case "monitor":
                g.DrawRoundedRectangleCompat(pen, 2, 3, 20, 15, 2);
                Line(8, 22, 16, 22); Line(12, 18, 12, 22);
                break;
            case "folder":
                using (var pathShape = new GraphicsPath())
                {
                    pathShape.AddLines(new[] { new PointF(3, 20), new PointF(3, 5), new PointF(10, 5), new PointF(12, 8), new PointF(21, 8), new PointF(21, 20), new PointF(3, 20) });
                    g.DrawPath(pen, pathShape);
                }
                break;
            case "download":
                g.DrawRoundedRectangleCompat(pen, 3, 4, 18, 16, 2);
                Line(12, 7, 12, 16); Line(8, 12, 12, 16); Line(16, 12, 12, 16);
                break;
            case "settings":
                // Gear outline for Windows Settings. Fit inside 24x24 viewbox.
                using (var gear = new GraphicsPath())
                {
                    var points = new List<PointF>();
                    for (int j = 0; j < 32; j++)
                    {
                        double angle = -Math.PI / 2 + j * Math.PI * 2 / 32;
                        double radius = j % 4 is 1 or 2 ? 10.0 : 7.6;
                        points.Add(new PointF(12 + (float)(radius * Math.Cos(angle)),
                                              12 + (float)(radius * Math.Sin(angle))));
                    }
                    gear.AddPolygon(points.ToArray());
                    g.DrawPath(pen, gear);
                    g.DrawEllipse(pen, 9, 9, 6, 6);
                }
                break;
            case "network":
                for (int i = 0; i < 5; i++) Line(3 + i * 4, 21 - i * 4, 3 + i * 4, 21);
                break;
            case "power":
                // Preview style: open ring with a clean top power stem.
                g.DrawArc(pen, 3, 3, 18, 18, -52, 284);
                Line(12, 2, 12, 10);
                break;
            case "restart":
                // Preview style: two circulating arrowheads, not a closed ring.
                g.DrawArc(pen, 3, 3, 18, 18, 212, 125);
                g.DrawArc(pen, 3, 3, 18, 18, 34, 125);
                Line(19, 4, 20, 10); Line(20, 10, 14, 9);
                Line(5, 20, 4, 14); Line(4, 14, 10, 15);
                break;
        }
        return bitmap;
    }
    private static void DrawRoundedRectangleCompat(this Graphics g, Pen p, float x, float y, float width, float height, float radius)
    {
        using var path = new GraphicsPath();
        path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
        path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
        path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        g.DrawPath(p, path);
    }
}
