using System.Drawing;
using System.IO;

namespace JanusDock;

/// <summary>Best-effort lookup of installed desktop programs; no fake shortcuts.</summary>
internal static class CommonApps
{
    internal sealed record Entry(string Name, string Exe, string[] Hints, string Category);
    internal static readonly Entry[] Catalog =
    {
        new("Microsoft Edge", "msedge.exe", new[]{"Microsoft Edge"}, "Navegadores"),
        new("Google Chrome", "chrome.exe", new[]{"Google Chrome"}, "Navegadores"),
        new("Brave", "brave.exe", new[]{"Brave"}, "Navegadores"),
        new("Mozilla Firefox", "firefox.exe", new[]{"Firefox"}, "Navegadores"),
        new("Word", "WINWORD.EXE", new[]{"Word"}, "Office"),
        new("Excel", "EXCEL.EXE", new[]{"Excel"}, "Office"),
        new("PowerPoint", "POWERPNT.EXE", new[]{"PowerPoint"}, "Office"),
        new("Outlook", "OUTLOOK.EXE", new[]{"Outlook"}, "Office"),
        new("OneNote", "ONENOTE.EXE", new[]{"OneNote"}, "Office"),
    };
    internal static string? Locate(Entry app)
    {
        // Prefer Start Menu shortcuts (also cover Click-to-Run layouts).
        foreach (var root in new[]{
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)})
        {
            try
            {
                if (!Directory.Exists(root)) continue;
                var links = Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories);
                string? match = links.FirstOrDefault(file => app.Hints.Any(h =>
                    Path.GetFileNameWithoutExtension(file).Equals(h, StringComparison.OrdinalIgnoreCase)));
                if (match != null) return match;
                match = links.FirstOrDefault(file => app.Hints.Any(h =>
                    Path.GetFileNameWithoutExtension(file).Contains(h, StringComparison.OrdinalIgnoreCase)));
                if (match != null) return match;
            }
            catch { }
        }
        var roots = new[]{
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        };
        string[] relative = app.Exe.ToLowerInvariant() switch {
            "msedge.exe" => new[]{"Microsoft/Edge/Application/msedge.exe"},
            "chrome.exe" => new[]{"Google/Chrome/Application/chrome.exe"},
            "brave.exe" => new[]{"BraveSoftware/Brave-Browser/Application/brave.exe"},
            "firefox.exe" => new[]{"Mozilla Firefox/firefox.exe"},
            _ => new[]{
                "Microsoft Office/root/Office16/" + app.Exe,
                "Microsoft Office/Office16/" + app.Exe,
                "Microsoft Office/Office15/" + app.Exe
            }
        };
        foreach (var root in roots)
            foreach (var suffix in relative)
            {
                try { string path = Path.Combine(root, suffix); if (File.Exists(path)) return path; }
                catch { }
            }
        return null;
    }
    internal static string? IconType(string path)
    {
        string basename = Path.GetFileName(path).ToLowerInvariant();
        string stem = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var app in Catalog)
                if (app.Hints.Any(h => stem.Contains(h, StringComparison.OrdinalIgnoreCase))) return app.Exe.ToLowerInvariant();
        }
        foreach (var app in Catalog) if (basename == app.Exe.ToLowerInvariant()) return basename;
        return null;
    }
}
