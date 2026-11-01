using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Media.Control;

namespace JanusDock;

/// <summary>
/// A compact Windows media-session controller. It reads the OS playback state
/// and track title (Spotify and any compatible player) without Spotify login.
/// Falls back to the system media keys if a session does not accept a command.
/// </summary>
public sealed class MediaTransportControl : Control
{
    const byte MediaPrevious = 0xB1, MediaPlayPause = 0xB3, MediaStop = 0xB2, MediaNext = 0xB0;
    const uint KeyUp = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extraInfo);

    readonly bool horizontal;
    readonly ToolTip tooltip = new();
    readonly System.Windows.Forms.Timer refreshTimer = new() { Interval = 850 };
    GlobalSystemMediaTransportControlsSessionManager? manager;
    GlobalSystemMediaTransportControlsSession? activeSession;
    GlobalSystemMediaTransportControlsSessionPlaybackStatus? lastStatus;
    string lastTitle = "";
    bool refreshing;
    bool commanding;
    bool isPlaying;

    /// <summary>Raised when Windows reports a new playing track or paused track resumes.</summary>
    public event EventHandler<string>? TrackStarted;

    public MediaTransportControl(bool horizontalBar)
    {
        horizontal = horizontalBar;
        DoubleBuffered = true;
        SetStyle(ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        AccessibleName = "Controles multimedia de Windows";
        Size = horizontal ? new Size(128, 33) : new Size(36, 128);
        tooltip.SetToolTip(this, "Anterior  |  Reproducir/Pausar  |  Detener  |  Siguiente");
        refreshTimer.Tick += async (_, _) => await RefreshSessionAsync();
        refreshTimer.Start();
    }

    static void SendMediaKey(byte key)
    {
        keybd_event(key, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, KeyUp, UIntPtr.Zero);
    }

    static bool Playing(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            return session.GetPlaybackInfo().PlaybackStatus ==
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        }
        catch { return false; }
    }

    async Task RefreshSessionAsync()
    {
        if (refreshing || IsDisposed || Disposing) return;
        refreshing = true;
        try
        {
            manager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var sessions = manager.GetSessions();
            // Pick a playing session; when paused retain the previous source,
            // so Play targets the same player rather than a random application.
            var selected = sessions.FirstOrDefault(Playing)
                ?? sessions.FirstOrDefault(s => activeSession != null &&
                    s.SourceAppUserModelId == activeSession.SourceAppUserModelId)
                ?? manager.GetCurrentSession()
                ?? sessions.FirstOrDefault();
            activeSession = selected;
            if (selected == null)
            {
                lastStatus = null;
                lastTitle = "";
                isPlaying = false;
                Invalidate();
                return;
            }

            var status = selected.GetPlaybackInfo().PlaybackStatus;
            bool nowPlaying = status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            string title = "";
            try
            {
                var properties = await selected.TryGetMediaPropertiesAsync();
                title = properties?.Title?.Trim() ?? "";
            }
            catch { /* Players may temporarily withhold metadata. */ }

            if (IsDisposed || Disposing) return;
            bool startOrChange = nowPlaying && title.Length > 0 &&
                (title != lastTitle || lastStatus !=
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing);
            lastTitle = title;
            lastStatus = status;
            isPlaying = nowPlaying;
            AccessibleDescription = nowPlaying ? "Pausar reproducción" : "Reanudar reproducción";
            tooltip.SetToolTip(this,
                nowPlaying ? "Anterior  |  Pausar  |  Detener  |  Siguiente" :
                             "Anterior  |  Reproducir  |  Detener  |  Siguiente");
            Invalidate();
            if (startOrChange) TrackStarted?.Invoke(this, title);
        }
        catch
        {
            // Windows may not have a current media session. Keep media keys
            // usable even if querying metadata or session access fails.
            activeSession = null;
            isPlaying = false;
            lastStatus = null;
            Invalidate();
        }
        finally { refreshing = false; }
    }

    async Task PressAsync(int index)
    {
        if (commanding) return;
        commanding = true;
        try
        {
            bool accepted = false;
            var session = activeSession;
            if (session != null)
            {
                try
                {
                    switch (index)
                    {
                        case 0:
                            accepted = await session.TrySkipPreviousAsync();
                            break;
                        case 1:
                            accepted = isPlaying ? await session.TryPauseAsync()
                                                 : await session.TryPlayAsync();
                            break;
                        case 2:
                            accepted = await session.TryStopAsync();
                            // Streaming clients may not implement Stop.
                            if (!accepted) accepted = await session.TryPauseAsync();
                            break;
                        case 3:
                            accepted = await session.TrySkipNextAsync();
                            break;
                    }
                }
                catch { accepted = false; }
            }
            if (!accepted)
                SendMediaKey(index switch
                {
                    0 => MediaPrevious,
                    1 => MediaPlayPause,
                    2 => MediaStop,
                    _ => MediaNext
                });
            if (index == 1 || index == 2)
            {
                // Immediate feedback, then synchronize with the player.
                isPlaying = index == 1 ? !isPlaying : false;
                Invalidate();
            }
            await RefreshSessionAsync();
        }
        finally { commanding = false; }
    }

    protected override async void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;
        int index = Math.Clamp(
            (int)((horizontal ? e.X / (float)Math.Max(1, Width)
                               : e.Y / (float)Math.Max(1, Height)) * 4), 0, 3);
        try { await PressAsync(index); }
        catch { /* A player can stop or disappear between click and dispatch. */ }
    }

    static GraphicsPath Rounded(RectangleF rect, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        var ink = BackColor.GetBrightness() > .55f
            ? Color.FromArgb(38, 58, 82) : Color.FromArgb(219, 229, 242);
        using var outline = new Pen(Color.FromArgb(88, 111, 133), 1.15f);
        using var icon = new Pen(ink, 1.9f)
        {
            StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round
        };
        using var brush = new SolidBrush(ink);
        float length = horizontal ? Width : Height;
        float step = length / 4f;
        using (var housing = Rounded(new RectangleF(1, 1, Width - 2, Height - 2), 7))
            g.DrawPath(outline, housing);
        for (int i = 1; i < 4; i++)
        {
            float cut = i * step;
            if (horizontal) g.DrawLine(outline, cut, 7, cut, Height - 7);
            else g.DrawLine(outline, 7, cut, Width - 7, cut);
        }
        for (int i = 0; i < 4; i++)
        {
            float x = horizontal ? (i + .5f) * step : Width / 2f;
            float y = horizontal ? Height / 2f : (i + .5f) * step;
            if (i == 1)
            {
                if (isPlaying)
                {
                    // The actual Windows playback state controls Pause vs Play.
                    g.FillRectangle(brush, x - 5, y - 7, 3, 14);
                    g.FillRectangle(brush, x + 2, y - 7, 3, 14);
                }
                else
                    g.DrawPolygon(icon, new PointF[]
                    {
                        new(x - 4, y - 7), new(x + 6, y), new(x - 4, y + 7)
                    });
            }
            else if (i == 2)
            {
                // Stop: a square, not two Pause bars.
                g.DrawRectangle(icon, x - 5, y - 5, 10, 10);
            }
            else if (i == 0)
            {
                // Previous faces LEFT; its stop bar is at the LEFT.
                g.DrawLine(icon, x - 8, y - 6, x - 8, y + 6);
                g.DrawPolygon(icon, new PointF[]
                {
                    new(x + 5, y - 6), new(x - 5, y), new(x + 5, y + 6)
                });
            }
            else
            {
                // Next faces RIGHT; its stop bar is at the RIGHT.
                g.DrawPolygon(icon, new PointF[]
                {
                    new(x - 5, y - 6), new(x + 5, y), new(x - 5, y + 6)
                });
                g.DrawLine(icon, x + 8, y - 6, x + 8, y + 6);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            refreshTimer.Stop();
            refreshTimer.Dispose();
            tooltip.Dispose();
        }
        base.Dispose(disposing);
    }
}
