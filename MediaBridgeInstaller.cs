using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace JanusDock;

/// <summary>
/// Installs our publisher-built media bridge once from GitHub. Windows SDK is
/// required to BUILD the helper; it must not be installed on the end-user PC.
/// </summary>
public static class MediaBridgeInstaller
{
    const string Version = "media-support-v2";
    const string PackageName = "JANUS-MediaBridge-v2.zip";
    const string ExeName = "JanusMediaBridge.exe";
    const string ReleaseUrl =
        "https://github.com/momocrackcorp/janus-dock/releases/download/" + Version + "/";

    static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JANUS", "Dock", "MediaBridge");
    public static string DirectoryPath => Path.Combine(Root, "v2");
    static string CurrentExePath => Path.Combine(DirectoryPath, ExeName);
    static string PreviousExePath => Path.Combine(Root, "v1", ExeName);

    static bool ValidFile(string path)
    {
        try { return File.Exists(path) && new FileInfo(path).Length > 100_000; }
        catch { return false; }
    }

    public static bool IsCurrentInstalled => ValidFile(CurrentExePath);
    public static bool HasPreviousVersion => ValidFile(PreviousExePath);
    public static bool IsInstalled => IsCurrentInstalled || HasPreviousVersion;
    // Keep v1 operating until the user installs v2.
    public static string ExePath => IsCurrentInstalled ? CurrentExePath : PreviousExePath;

    public static async Task InstallAsync(IProgress<string>? progress = null)
    {
        if (IsCurrentInstalled) return;
        Directory.CreateDirectory(DirectoryPath);
        progress?.Report("Descargando componente multimedia desde GitHub...");
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("JANUS-Dock/6.4.87");
        var manifest = await http.GetStringAsync(ReleaseUrl + "SHA256SUMS.txt");
        var checksumLine = manifest.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).FirstOrDefault(x => x.EndsWith(PackageName, StringComparison.Ordinal));
        if (checksumLine == null || checksumLine.Length < 64)
            throw new InvalidDataException("No se encontró el checksum del archivo en GitHub.");
        var expected = checksumLine[..64].ToLowerInvariant();
        if (!expected.All(Uri.IsHexDigit))
            throw new InvalidDataException("El checksum publicado no es válido.");
        byte[] bytes = await http.GetByteArrayAsync(ReleaseUrl + PackageName);
        string actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new InvalidDataException("La descarga no coincide con el SHA-256 publicado.");
        progress?.Report("Verificando e instalando componente multimedia...");
        using var stream = new MemoryStream(bytes, writable: false);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var file = zip.Entries.FirstOrDefault(x =>
            x.FullName == ExeName && x.Length > 100_000);
        if (file == null)
            throw new InvalidDataException("El componente descargado no contiene el archivo esperado.");
        string temporary = CurrentExePath + ".download";
        try
        {
            using (var input = file.Open())
            using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                await input.CopyToAsync(output);
            File.Move(temporary, CurrentExePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        progress?.Report("Componente multimedia instalado.");
    }
}
