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
    const string Version = "media-support-v1";
    const string PackageName = "JANUS-MediaBridge-v1.zip";
    const string ExeName = "JanusMediaBridge.exe";
    const string ReleaseUrl =
        "https://github.com/momocrackcorp/janus-dock/releases/download/" + Version + "/";

    public static string DirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JANUS", "Dock", "MediaBridge", "v1");

    public static string ExePath => Path.Combine(DirectoryPath, ExeName);
    public static bool IsInstalled
    {
        get
        {
            try { return File.Exists(ExePath) && new FileInfo(ExePath).Length > 100_000; }
            catch { return false; }
        }
    }

    public static async Task InstallAsync(IProgress<string>? progress = null)
    {
        if (IsInstalled) return;
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
        string temporary = ExePath + ".download";
        try
        {
            using (var input = file.Open())
            using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                await input.CopyToAsync(output);
            File.Move(temporary, ExePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        progress?.Report("Componente multimedia instalado.");
    }
}
