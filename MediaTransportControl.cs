using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace JanusDock;

/// <summary>
/// Lightweight UI with six actions. Optional metadata/state arrives from the
/// separately downloaded MediaBridge executable over stdin/stdout.
/// </summary>
public sealed class MediaTransportControl : Control
{
    const byte VkPrev = 0xB1, VkPlayPause = 0xB3, VkStop = 0xB2,
        VkNext = 0xB0, VkVolumeDown = 0xAE, VkVolumeUp = 0xAF;
    const uint KeyReleased = 0x0002;
    [DllImport("user32.dll", SetLastError = true)]
    static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);

    readonly bool horizontal;
    readonly ToolTip tips = new();
    Process? helper;
    bool playing;
    string title = "";
    bool hadState;
    public event EventHandler<string>? TrackStarted;

    public MediaTransportControl(bool horizontalBar)
    {
        horizontal = horizontalBar;
        Size = horizontalBar ? new Size(174, 32) : new Size(35, 174);
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        AccessibleName = "Controles de reproducción y volumen";
        tips.SetToolTip(this,
            "Anterior | Reproducir/Pausar | Detener | Siguiente | Bajar volumen | Subir volumen");
    }

    static void Key(byte code)
    {
        keybd_event(code, 0, 0, UIntPtr.Zero);
        keybd_event(code, 0, KeyReleased, UIntPtr.Zero);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        StartHelper();
    }

    void StartHelper()
    {
        if (!MediaBridgeInstaller.IsInstalled || helper != null) return;
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(MediaBridgeInstaller.ExePath)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardInputEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };
            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data == null || IsDisposed || !IsHandleCreated) return;
                try { BeginInvoke(new Action(() => HandleLine(args.Data))); }
                catch (InvalidOperationException) { }
            };
            if (!process.Start()) { process.Dispose(); return; }
            process.BeginOutputReadLine();
            // Drain stderr to prevent any pipe deadlock.
            process.BeginErrorReadLine();
            helper = process;
        }
        catch { helper?.Dispose(); helper = null; }
    }

    void HandleLine(string message)
    {
        if (IsDisposed) return;
        if (message.StartsWith("FALLBACK", StringComparison.Ordinal))
        {
            string command = message.Contains('\t') ? message[(message.IndexOf('\t') + 1)..] : "";
            Key(command switch
            {
                "PREV" => VkPrev, "PLAY" or "PAUSE" => VkPlayPause,
                "STOP" => VkStop, "NEXT" => VkNext, _ => VkPlayPause
            });
            return;
        }
        if (!message.StartsWith("STATE\t", StringComparison.Ordinal)) return;
        var sections = message.Split('\t', 3);
        if (sections.Length != 3) return;
        bool nextPlaying = sections[1] == "1";
        string nextTitle = "";
        try { nextTitle = Encoding.UTF8.GetString(Convert.FromBase64String(sections[2])); }
        catch { }
        bool shouldShow = nextPlaying && !string.IsNullOrWhiteSpace(nextTitle) &&
            (!hadState || !playing || nextTitle != title);
        playing = nextPlaying;
        title = nextTitle;
        hadState = true;
        Invalidate();
        if (shouldShow) TrackStarted?.Invoke(this, nextTitle);
    }

    void SendCommand(string command, byte fallback)
    {
        if (helper?.HasExited == false)
        {
            try
            {
                helper.StandardInput.WriteLine(command);
                helper.StandardInput.Flush();
                return;
            }
            catch { /* If helper exits, Windows keys remain operational. */ }
        }
        Key(fallback);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;
        int index = Math.Clamp(
            (int)((horizontal ? e.X / (float)Math.Max(1, Width)
                               : e.Y / (float)Math.Max(1, Height)) * 6), 0, 5);
        switch (index)
        {
            case 0: SendCommand("PREV", VkPrev); break;
            case 1: SendCommand(playing ? "PAUSE" : "PLAY", VkPlayPause); break;
            case 2: SendCommand("STOP", VkStop); break;
            case 3: SendCommand("NEXT", VkNext); break;
            case 4: Key(VkVolumeDown); break; // OS volume, no SDK dependency
            case 5: Key(VkVolumeUp); break;
        }
        if (index == 1 || index == 2)
        {
            playing = index == 1 ? !playing : false;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var tint = BackColor.GetBrightness() > .55f
            ? Color.FromArgb(38, 58, 82) : Color.FromArgb(215, 225, 239);
        using var stroke = new Pen(tint, 1.8f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var ring = new Pen(Color.FromArgb(84, 110, 136), 1.1f);
        using var ink = new SolidBrush(tint);
        float distance = horizontal ? Width : Height, step = distance / 6f;
        var bounds = new RectangleF(1, 1, Width - 2, Height - 2);
        using (var path = new GraphicsPath())
        {
            float r = 7, d = 14;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.DrawPath(ring, path);
        }
        for (int i = 1; i < 6; i++)
        {
            float pos = i * step;
            if (horizontal) g.DrawLine(ring, pos, 6, pos, Height - 6);
            else g.DrawLine(ring, 6, pos, Width - 6, pos);
        }
        for (int i = 0; i < 6; i++)
        {
            float x = horizontal ? (i + .5f) * step : Width / 2f;
            float y = horizontal ? Height / 2f : (i + .5f) * step;
            switch (i)
            {
                case 0:
                    g.DrawLine(stroke, x - 7, y - 6, x - 7, y + 6);
                    g.DrawPolygon(stroke, new PointF[]
                    { new(x + 5, y - 6), new(x - 5, y), new(x + 5, y + 6) });
                    break;
                case 1:
                    if (playing)
                    {
                        g.FillRectangle(ink, x - 5, y - 6, 3, 12);
                        g.FillRectangle(ink, x + 2, y - 6, 3, 12);
                    }
                    else
                        g.DrawPolygon(stroke, new PointF[]
                        { new(x - 4, y - 6), new(x + 6, y), new(x - 4, y + 6) });
                    break;
                case 2: g.DrawRectangle(stroke, x - 5, y - 5, 10, 10); break;
                case 3:
                    g.DrawPolygon(stroke, new PointF[]
                    { new(x - 5, y - 6), new(x + 5, y), new(x - 5, y + 6) });
                    g.DrawLine(stroke, x + 7, y - 6, x + 7, y + 6);
                    break;
                case 4:
                case 5:
                    g.DrawPolygon(stroke, new PointF[]
                    { new(x - 7, y - 3), new(x - 4, y - 3), new(x, y - 7),
                      new(x, y + 7), new(x - 4, y + 3), new(x - 7, y + 3) });
                    if (i == 4) g.DrawLine(stroke, x + 4, y, x + 9, y);
                    else
                    {
                        g.DrawLine(stroke, x + 4, y, x + 9, y);
                        g.DrawLine(stroke, x + 6.5f, y - 3, x + 6.5f, y + 3);
                    }
                    break;
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                if (helper?.HasExited == false)
                {
                    helper.StandardInput.WriteLine("QUIT");
                    if (!helper.WaitForExit(1000)) helper.Kill(entireProcessTree: true);
                }
            }
            catch { }
            helper?.Dispose();
            tips.Dispose();
        }
        base.Dispose(disposing);
    }
}
