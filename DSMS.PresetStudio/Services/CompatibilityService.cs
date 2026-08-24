using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DSMS.PresetStudio.Services;

public sealed class CompatibilityProfile
{
    public int[] SupportedJsonSchemas { get; set; } = [3];
    public string MinimumModLoaderVersion { get; set; } = "0.7.0";
    public string[] TestedModLoaderVersions { get; set; } = ["0.7.0", "0.7.1"];
    public string RecommendedModLoaderVersion { get; set; } = "0.7.1";
    public string StudioReleaseRepository { get; set; } = "Hoverbike51/DSMS-Preset-Studio";
}

public sealed record ModLoaderStatus(string? Version, string State, string Message, string? ScriptsPath)
{
    public bool Found => Version is not null;
}

public static partial class CompatibilityService
{
    public static CompatibilityProfile Load()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "compatibility.json");
            return JsonSerializer.Deserialize<CompatibilityProfile>(File.ReadAllText(path), JsonOptions) ?? new();
        }
        catch { return new(); }
    }

    public static ModLoaderStatus Detect(string? configuredPath, CompatibilityProfile profile)
    {
        foreach (var path in CandidatePaths(configuredPath))
        {
            var version = ReadVersion(path);
            if (version is null) continue;
            if (!Version.TryParse(version.TrimStart('v', 'V'), out var installed))
                return new(version, "UNKNOWN", $"DSMS ModLoader {version} found, but its version cannot be compared.", path);

            Version.TryParse(profile.MinimumModLoaderVersion, out var minimum);
            Version.TryParse(profile.RecommendedModLoaderVersion, out var recommended);
            if (minimum is not null && installed < minimum)
                return new(version, "TOO OLD", $"Installed v{version}; minimum compatible version is v{profile.MinimumModLoaderVersion}.", path);
            if (profile.TestedModLoaderVersions.Contains(version, StringComparer.OrdinalIgnoreCase))
                return new(version, "COMPATIBLE", $"Installed v{version}; tested with JSON v3.", path);
            if (recommended is not null && installed > recommended)
                return new(version, "NEWER", $"Installed v{version}; newer than the versions tested by this Studio build.", path);
            return new(version, "COMPATIBLE", $"Installed v{version}; meets the minimum compatibility requirement.", path);
        }
        return new(null, "NOT FOUND", "DSMS ModLoader was not found. Select its Scripts folder in Settings.", null);
    }

    private static IEnumerable<string> CandidatePaths(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath)) yield return NormalizeScriptsPath(configuredPath);
        var roots = new[] { @"C:\", @"D:\", @"E:\", @"F:\", @"S:\" };
        foreach (var root in roots)
        {
            yield return Path.Combine(root, @"SteamLibrary\steamapps\common\DragonSword  Awakening\DS\Binaries\Win64\ue4ss\Mods\HMVDSMeshSelector\Scripts");
            yield return Path.Combine(root, @"SteamLibrary\steamapps\common\DragonSword Awakening\DS\Binaries\Win64\ue4ss\Mods\HMVDSMeshSelector\Scripts");
        }
    }

    public static string NormalizeScriptsPath(string path)
    {
        path = Path.GetFullPath(path.Trim().Trim('"'));
        if (File.Exists(Path.Combine(path, "main.lua"))) return path;
        var nested = Path.Combine(path, "Scripts");
        return Directory.Exists(nested) ? nested : path;
    }

    private static string? ReadVersion(string scriptsPath)
    {
        try
        {
            var versionJson = Path.Combine(Directory.GetParent(scriptsPath)?.FullName ?? scriptsPath, "version.json");
            if (File.Exists(versionJson))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(versionJson));
                if (document.RootElement.TryGetProperty("Version", out var upper)) return upper.GetString();
                if (document.RootElement.TryGetProperty("version", out var lower)) return lower.GetString();
            }
            var main = Path.Combine(scriptsPath, "main.lua");
            if (!File.Exists(main)) return null;
            var match = VersionPattern().Match(File.ReadAllText(main));
            return match.Success ? match.Groups[1].Value : null;
        }
        catch { return null; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [GeneratedRegex("local\\s+VERSION\\s*=\\s*['\\\"]([^'\\\"]+)['\\\"]", RegexOptions.IgnoreCase)]
    private static partial Regex VersionPattern();
}
