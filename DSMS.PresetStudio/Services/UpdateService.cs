using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace DSMS.PresetStudio.Services;

public sealed record StudioUpdate(string Version, string ReleaseUrl, string AssetName, string AssetUrl, string? Digest, string? ChecksumUrl)
{
    public bool IsNewer => System.Version.TryParse(Version.TrimStart('v', 'V'), out var available) &&
                           System.Version.TryParse(AppVersion.Current, out var current) && available > current;
}

public sealed record PreparedUpdate(StudioUpdate Update, string SourceDirectory);

public static class UpdateService
{
    private static readonly HttpClient Client = CreateClient();

    public static async Task<StudioUpdate?> CheckAsync(string repository, CancellationToken cancellationToken = default)
    {
        using var response = await Client.GetAsync($"https://api.github.com/repos/{repository}/releases/latest", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new InvalidOperationException("No public GitHub release is available yet, or the repository is not public.");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "0.0.0";
        var page = root.GetProperty("html_url").GetString() ?? $"https://github.com/{repository}/releases";
        var assets = root.GetProperty("assets").EnumerateArray().Select(x => new
        {
            Name = x.GetProperty("name").GetString() ?? "",
            Url = x.GetProperty("browser_download_url").GetString() ?? "",
            Digest = x.TryGetProperty("digest", out var digest) ? digest.GetString() : null
        }).ToArray();
        var zip = assets.FirstOrDefault(x => x.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                                             x.Name.Contains("Preset-Studio", StringComparison.OrdinalIgnoreCase));
        if (zip is null) return null;
        var checksum = assets.FirstOrDefault(x => x.Name.Contains("SHA256", StringComparison.OrdinalIgnoreCase) ||
                                                   x.Name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase));
        return new(tag.TrimStart('v', 'V'), page, zip.Name, zip.Url, zip.Digest, checksum?.Url);
    }

    public static async Task<PreparedUpdate> DownloadAsync(StudioUpdate update, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var work = Path.Combine(Path.GetTempPath(), "DSMSPresetStudioUpdate", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        var archive = Path.Combine(work, update.AssetName);
        progress?.Report("Downloading release package…");
        await using (var input = await Client.GetStreamAsync(update.AssetUrl, cancellationToken))
        await using (var output = File.Create(archive)) await input.CopyToAsync(output, cancellationToken);

        progress?.Report("Verifying SHA-256…");
        var expected = ParseDigest(update.Digest);
        if (expected is null && update.ChecksumUrl is not null)
        {
            var text = await Client.GetStringAsync(update.ChecksumUrl, cancellationToken);
            expected = text.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(x => x.Length == 64 && x.All(Uri.IsHexDigit));
        }
        if (expected is null) throw new InvalidDataException("The GitHub release does not provide a SHA-256 digest. Installation was cancelled.");
        await using var archiveStream = File.OpenRead(archive);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(archiveStream, cancellationToken));
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The downloaded archive failed SHA-256 verification. Installation was cancelled.");

        progress?.Report("Preparing update…");
        var extracted = Path.Combine(work, "extracted");
        ZipFile.ExtractToDirectory(archive, extracted);
        var executable = Directory.EnumerateFiles(extracted, "DSMS.PresetStudio.exe", SearchOption.AllDirectories).FirstOrDefault()
                         ?? Directory.EnumerateFiles(extracted, "DSMS Preset Studio.exe", SearchOption.AllDirectories).FirstOrDefault()
                         ?? throw new InvalidDataException("The release archive does not contain DSMS Preset Studio.");
        return new(update, Path.GetDirectoryName(executable)!);
    }

    public static void LaunchInstaller(PreparedUpdate prepared)
    {
        var currentExecutable = Environment.ProcessPath ?? throw new InvalidOperationException("The running executable path is unavailable.");
        var updaterDirectory = Path.Combine(Path.GetTempPath(), "DSMSPresetStudioUpdater", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(updaterDirectory);
        var updaterExecutable = Path.Combine(updaterDirectory, Path.GetFileName(currentExecutable));
        File.Copy(currentExecutable, updaterExecutable);
        var start = new ProcessStartInfo(updaterExecutable) { UseShellExecute = false };
        start.ArgumentList.Add("--apply-update");
        start.ArgumentList.Add(Environment.ProcessId.ToString());
        start.ArgumentList.Add(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        start.ArgumentList.Add(prepared.SourceDirectory);
        _ = Process.Start(start) ?? throw new InvalidOperationException("The update helper could not be started.");
    }

    private static string? ParseDigest(string? digest)
    {
        if (string.IsNullOrWhiteSpace(digest)) return null;
        var value = digest.Contains(':') ? digest[(digest.IndexOf(':') + 1)..] : digest;
        return value.Length == 64 && value.All(Uri.IsHexDigit) ? value : null;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"DSMS-Preset-Studio/{AppVersion.Current}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }
}
