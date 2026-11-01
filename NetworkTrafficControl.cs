using System.Drawing;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>Rolling 60-second average throughput of the selected network adapter.</summary>
public sealed class NetworkTrafficControl : Control
{
    readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    NetworkInterface? active;
    long lastReceived, lastSent;
    DateTime lastSample;
    double down, up;
    readonly Queue<(DateTime End, double Seconds, double Received, double Sent)> history = new();
    const double WindowSeconds = 60;
    public bool Horizontal { get; }
    public string DisplayMode { get; set; } = "Texto";
    public string BackgroundMode { get; set; } = "Barra";
    static readonly Image? NetworkIcon = LucideDockIcons.Load("janus:network-status", false);
    public NetworkTrafficControl(bool horizontal)
    {
        Horizontal = horizontal;
        Size = horizontal ? new Size(170, 58) : new Size(80, 176);
        Margin = new Padding(3, 2, 3, 2);
        DoubleBuffered = true;
        timer.Tick += (_, _) => Sample();
        timer.Start();
        Sample();
    }
    static NetworkInterface? Primary()
    {
        // A connected adapter carrying a default gateway is preferred to virtual,
        // disconnected and loopback adapters. When multiple qualify, select the
        // fastest one; Windows routing may still choose a different interface.
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .Where(n => {
                try { return n.GetIPProperties().GatewayAddresses.Any(g =>
                    g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                    !g.Address.Equals(System.Net.IPAddress.Any)); }
                catch { return false; }
            })
            .OrderByDescending(n => n.Speed).FirstOrDefault();
    }
    void Sample()
    {
        try
        {
            var current = Primary();
            if (current is null)
            {
                active = null; lastSample = default; history.Clear(); down = up = 0; Invalidate(); return;
            }
            var stats = current.GetIPStatistics();
            var now = DateTime.UtcNow;
            if (active?.Id != current.Id || lastSample == default)
            {
                active = current;
                lastReceived = stats.BytesReceived; lastSent = stats.BytesSent;
                lastSample = now; history.Clear(); down = up = 0;
            }
            else
            {
                double seconds = (now - lastSample).TotalSeconds;
                // Keep the sample duration so delayed UI ticks are averaged by time,
                // not by the number of callbacks.
                if (seconds > 0 && seconds <= WindowSeconds)
                {
                    long receivedDelta = stats.BytesReceived - lastReceived;
                    long sentDelta = stats.BytesSent - lastSent;
                    if (receivedDelta < 0 || sentDelta < 0) history.Clear(); // Counter reset.
                    else history.Enqueue((now, seconds, receivedDelta, sentDelta));
                }
                else if (seconds > WindowSeconds) history.Clear();
                lastReceived = stats.BytesReceived; lastSent = stats.BytesSent; lastSample = now;

                // Remove samples that fall entirely outside the trailing 60 seconds.
                DateTime cutoff = now.AddSeconds(-WindowSeconds);
                while (history.Count > 0 && history.Peek().End <= cutoff)
                    history.Dequeue();
                double sumDown = 0, sumUp = 0, duration = 0;
                foreach (var sample in history)
                {
                    double overlap = (sample.End - (sample.End.AddSeconds(-sample.Seconds) > cutoff
                        ? sample.End.AddSeconds(-sample.Seconds) : cutoff)).TotalSeconds;
                    if (overlap <= 0) continue;
                    double fraction = Math.Min(1, overlap / sample.Seconds);
                    sumDown += sample.Received * fraction;
                    sumUp += sample.Sent * fraction;
                    duration += overlap;
                }
                down = duration > 0 ? sumDown / duration : 0;
                up = duration > 0 ? sumUp / duration : 0;
            }
            AccessibleDescription = $"Promedio de red de los últimos 60 segundos: descarga {Format(down)}, subida {Format(up)}";
            Invalidate();
        }
        catch { down = up = 0; Invalidate(); }
    }
    static string Format(double bytes) => bytes >= 1048576
        ? $"{bytes / 1048576:0.00} MB/s" : $"{bytes / 1024:0.0} KB/s";
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        if (DisplayMode == "Texto")
        {
            // Minimal network readout: true Lucide network symbol, plus
            // only the send/receive arrows and values. No signal bars.
            if (Horizontal)
            {
                using var surfacePath = new System.Drawing.Drawing2D.GraphicsPath();
                int r = Math.Min(11, Height / 4);
                surfacePath.AddArc(2, 2, r * 2, r * 2, 180, 90);
                surfacePath.AddArc(Width - 2 - r * 2, 2, r * 2, r * 2, 270, 90);
                surfacePath.AddArc(Width - 2 - r * 2, Height - 2 - r * 2, r * 2, r * 2, 0, 90);
                surfacePath.AddArc(2, Height - 2 - r * 2, r * 2, r * 2, 90, 90);
                surfacePath.CloseFigure();
                if (BackgroundMode == "Oscuro")
                {
                    using var background = new SolidBrush(Color.FromArgb(26, 32, 45));
                    g.FillPath(background, surfacePath);
                }
                if (NetworkIcon != null)
                    g.DrawImage(NetworkIcon, new Rectangle(9, (Height - 25) / 2, 25, 25));
                using var textFont = new Font("Segoe UI", 11.5f, FontStyle.Bold, GraphicsUnit.Point);
                var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
                    TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
                int mid = Height / 2;
                TextRenderer.DrawText(g, "↑ " + Format(up), textFont,
                    new Rectangle(42, 1, Math.Max(25, Width - 46), mid - 1),
                    Color.FromArgb(48, 204, 239), flags);
                TextRenderer.DrawText(g, "↓ " + Format(down), textFont,
                    new Rectangle(42, mid, Math.Max(25, Width - 46), Height - mid - 1),
                    Color.FromArgb(78, 227, 126), flags);
            }
            else
            {
                if (NetworkIcon != null)
                    g.DrawImage(NetworkIcon, new Rectangle((Width - 26) / 2, 6, 26, 26));
                using var textFont = new Font("Segoe UI", 9f, FontStyle.Bold);
                var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding |
                    TextFormatFlags.WordBreak;
                TextRenderer.DrawText(g, "↑ " + Format(up), textFont,
                    new Rectangle(2, 45, Width - 4, 52),
                    Color.FromArgb(48, 204, 239), flags);
                TextRenderer.DrawText(g, "↓ " + Format(down), textFont,
                    new Rectangle(2, 110, Width - 4, 52),
                    Color.FromArgb(78, 227, 126), flags);
            }
            return;
        }
        if (Horizontal)
        {
            // Independent monitor, always immediately LEFT of the end capsule.
            // The dark rectangular surface is intentionally NOT a part of it.
            int pad = 2, corner = Math.Min(11, Height / 4);
            using var panelShape = new System.Drawing.Drawing2D.GraphicsPath();
            panelShape.AddArc(pad, pad, corner * 2, corner * 2, 180, 90);
            panelShape.AddArc(Width - pad - corner * 2, pad, corner * 2, corner * 2, 270, 90);
            panelShape.AddArc(Width - pad - corner * 2, Height - pad - corner * 2, corner * 2, corner * 2, 0, 90);
            panelShape.AddArc(pad, Height - pad - corner * 2, corner * 2, corner * 2, 90, 90);
            panelShape.CloseFigure();
            if (BackgroundMode == "Oscuro")
            {
                using var surface = new SolidBrush(Color.FromArgb(26, 32, 45));
                g.FillPath(surface, panelShape);
            }
            // Amber stepped bandwidth glyph, scaled to the dock height.
            int barBottom = Height - 8;
            int maxBar = Math.Max(15, Height - 16);
            for (int i = 0; i < 5; i++)
            {
                int bh = (i + 1) * maxBar / 5;
                var color = Color.FromArgb(255, Math.Min(245, 174 + i * 13), Math.Max(88, 156 - i * 8));
                using var brush = new SolidBrush(color);
                g.FillRectangle(brush, 7 + i * 7, barBottom - bh, 5, bh);
            }
            using var networkFont = new Font("Segoe UI", 12.2f, FontStyle.Bold, GraphicsUnit.Point);
            var flag = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                       TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
            int ySplit = Height / 2;
            TextRenderer.DrawText(g, "↑ " + Format(up), networkFont,
                new Rectangle(45, 2, Math.Max(28, Width - 51), Math.Max(16, ySplit - 2)),
                Color.FromArgb(39, 202, 233), flag);
            TextRenderer.DrawText(g, "↓ " + Format(down), networkFont,
                new Rectangle(45, ySplit, Math.Max(28, Width - 51), Math.Max(16, Height - ySplit - 2)),
                Color.FromArgb(75, 233, 134), flag);
            return;
        }
        var downColor = Color.FromArgb(63, 215, 138);
        var upColor = Color.FromArgb(105, 177, 245);
        var labelColor = ForeColor;
        using var legacyFont = new Font("Segoe UI", Horizontal ? 9f : 9.5f, FontStyle.Bold);
        using var smallFont = new Font("Segoe UI", 8f);
        using var signalPen = new Pen(Color.FromArgb(236, 191, 82), 2.8f);
        // Retro five-step signal strength silhouette; purely decorative,
        // not a signal-quality or bandwidth measurement.
        int left = Horizontal ? 8 : (Width - 24) / 2, top = Horizontal ? 17 : 7;
        for (int i = 0; i < 5; i++)
            g.DrawLine(signalPen, left + i * 5, top + 22 - i * 4, left + i * 5, top + 24);
        if (Horizontal)
        {
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
            int textX = 42;
            int lineHeight = Math.Max(16, Height / 2);
            int textWidth = Math.Max(40, Width - textX - 4);
            TextRenderer.DrawText(g, "↑ " + Format(up), legacyFont, new Rectangle(textX, 0, textWidth, lineHeight), upColor, flags);
            TextRenderer.DrawText(g, "↓ " + Format(down), legacyFont, new Rectangle(textX, lineHeight, textWidth, lineHeight), downColor, flags);
        }
        else
        {
            TextRenderer.DrawText(g, "↓", legacyFont, new Rectangle(0, 40, Width, 22), downColor, TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, Format(down).Replace(" ", "\n"), smallFont, new Rectangle(0, 62, Width, 43), labelColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
            TextRenderer.DrawText(g, "↑", legacyFont, new Rectangle(0, 108, Width, 22), upColor, TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, Format(up).Replace(" ", "\n"), smallFont, new Rectangle(0, 130, Width, 43), labelColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Stop(); timer.Dispose(); }
        base.Dispose(disposing);
    }
}
