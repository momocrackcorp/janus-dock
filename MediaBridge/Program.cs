using System;
using System.Linq;
using System.Text;
using Windows.Media.Control;

// Separate optional helper: no Windows.SDK.NET.dll in JanusDock.exe.
internal static class Program
{
    static GlobalSystemMediaTransportControlsSessionManager? manager;
    static GlobalSystemMediaTransportControlsSession? lastSession;
    static readonly SemaphoreSlim gate = new(1);

    static bool Playing(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            return session.GetPlaybackInfo().PlaybackStatus ==
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        }
        catch { return false; }
    }

    static GlobalSystemMediaTransportControlsSession? Current()
    {
        if (manager == null) return null;
        var sessions = manager.GetSessions();
        return sessions.FirstOrDefault(Playing)
            ?? sessions.FirstOrDefault(s => lastSession != null &&
                s.SourceAppUserModelId == lastSession.SourceAppUserModelId)
            ?? manager.GetCurrentSession()
            ?? sessions.FirstOrDefault();
    }

    static async Task Main()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        try { manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync(); }
        catch { }
        var commands = Task.Run(async () =>
        {
            string? line;
            while ((line = await Console.In.ReadLineAsync()) != null)
            {
                if (line == "QUIT") break;
                await gate.WaitAsync();
                try
                {
                    var session = Current();
                    if (session == null) { Console.WriteLine("FALLBACK\t" + line); continue; }
                    bool accepted = line switch
                    {
                        "PREV" => await session.TrySkipPreviousAsync(),
                        "NEXT" => await session.TrySkipNextAsync(),
                        "PAUSE" => await session.TryPauseAsync(),
                        "PLAY" => await session.TryPlayAsync(),
                        "STOP" => await session.TryStopAsync(),
                        _ => false
                    };
                    if (!accepted && line == "STOP")
                        accepted = await session.TryPauseAsync();
                    if (!accepted) Console.WriteLine("FALLBACK\t" + line);
                }
                catch { Console.WriteLine("FALLBACK\t" + line); }
                finally { gate.Release(); }
            }
        });

        while (!commands.IsCompleted)
        {
            await gate.WaitAsync();
            try
            {
                var session = Current();
                lastSession = session;
                if (session == null) Console.WriteLine("STATE\t0\t");
                else
                {
                    bool playing = Playing(session);
                    string title = "";
                    try
                    {
                        var metadata = await session.TryGetMediaPropertiesAsync();
                        title = metadata?.Title ?? "";
                    }
                    catch { }
                    Console.WriteLine("STATE\t" + (playing ? "1" : "0") + "\t" +
                        Convert.ToBase64String(Encoding.UTF8.GetBytes(title)));
                }
            }
            catch { Console.WriteLine("STATE\t0\t"); }
            finally { gate.Release(); }
            await Task.Delay(850);
        }
    }
}
