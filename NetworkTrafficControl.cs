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
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var downColor = Color.FromArgb(63, 215, 138);
        var upColor = Color.FromArgb(105, 177, 245);
        var labelColor = ForeColor;
        using var textFont = new Font("Segoe UI", Horizontal ? 9f : 9.5f, FontStyle.Bold);
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
            TextRenderer.DrawText(g, "↑ " + Format(up), textFont, new Rectangle(textX, 0, textWidth, lineHeight), upColor, flags);
            TextRenderer.DrawText(g, "↓ " + Format(down), textFont, new Rectangle(textX, lineHeight, textWidth, lineHeight), downColor, flags);
        }
        else
        {
            TextRenderer.DrawText(g, "↓", textFont, new Rectangle(0, 40, Width, 22), downColor, TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, Format(down).Replace(" ", "\n"), smallFont, new Rectangle(0, 62, Width, 43), labelColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
            TextRenderer.DrawText(g, "↑", textFont, new Rectangle(0, 108, Width, 22), upColor, TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, Format(up).Replace(" ", "\n"), smallFont, new Rectangle(0, 130, Width, 43), labelColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Stop(); timer.Dispose(); }
        base.Dispose(disposing);
    }
}
