using System.Drawing;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>Live bytes-per-second from one active default-gateway adapter. No external service.</summary>
public sealed class NetworkTrafficControl : Control
{
    readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    NetworkInterface? active;
    long lastReceived, lastSent;
    DateTime lastSample;
    double down, up;
    public bool Horizontal { get; }
    public NetworkTrafficControl(bool horizontal)
    {
        Horizontal = horizontal;
        Size = horizontal ? new Size(340, 58) : new Size(80, 176);
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
                active = null; down = up = 0; Invalidate(); return;
            }
            var stats = current.GetIPStatistics();
            var now = DateTime.UtcNow;
            if (active?.Id != current.Id || lastSample == default)
            {
                active = current;
                lastReceived = stats.BytesReceived; lastSent = stats.BytesSent;
                lastSample = now; down = up = 0;
            }
            else
            {
                double seconds = (now - lastSample).TotalSeconds;
                if (seconds > 0)
                {
                    down = Math.Max(0, stats.BytesReceived - lastReceived) / seconds;
                    up = Math.Max(0, stats.BytesSent - lastSent) / seconds;
                }
                lastReceived = stats.BytesReceived; lastSent = stats.BytesSent; lastSample = now;
            }
            AccessibleDescription = $"Red: descarga {Format(down)}, subida {Format(up)}";
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
            // Measure each localized string and reserve independent text regions.
            // Never draw the upload label at a fixed X that overlaps download.
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            string downloadText = "↓ " + Format(down);
            string uploadText = "↑ " + Format(up);
            int gap = 14;
            int startX = 43;
            int available = Math.Max(40, Width - startX - 5);
            var downSize = TextRenderer.MeasureText(g, downloadText, textFont, new Size(int.MaxValue, 32), flags);
            var upSize = TextRenderer.MeasureText(g, uploadText, textFont, new Size(int.MaxValue, 32), flags);
            int combined = downSize.Width + gap + upSize.Width;
            float scale = combined > available ? Math.Max(0.66f, (float)available / combined) : 1f;
            using var fitted = new Font("Segoe UI", 9f * scale, FontStyle.Bold);
            downSize = TextRenderer.MeasureText(g, downloadText, fitted, new Size(int.MaxValue, 32), flags);
            int uploadX = startX + downSize.Width + gap;
            TextRenderer.DrawText(g, downloadText, fitted, new Point(startX, 17), downColor, flags);
            TextRenderer.DrawText(g, uploadText, fitted, new Point(uploadX, 17), upColor, flags);
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
