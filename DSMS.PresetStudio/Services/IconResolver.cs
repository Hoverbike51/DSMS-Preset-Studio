using System.IO;
using System.Text.RegularExpressions;
using DSMS.Core.Models;

namespace DSMS.PresetStudio.Services;

public sealed record IconResolution(string FilePath, string Source, string Message, string? SuggestedIconPath = null)
{
    public bool HasSuggestion => !string.IsNullOrWhiteSpace(SuggestedIconPath);
}

public static partial class IconResolver
{
    public static string IconsDirectory => Path.Combine(AppContext.BaseDirectory, "Icons");
    public static string ImportedDirectory => Path.Combine(IconsDirectory, "Imported");

    public static IconResolution Resolve(PresetDocument preset, string? fmodelRoot)
    {
        Directory.CreateDirectory(ImportedDirectory);
        var iconPath = preset.IconPath?.Trim();
        var generic = GenericPath(preset.Type);
        if (string.IsNullOrWhiteSpace(iconPath))
            return new(generic, "Generic", "IconPath is empty; a generic icon is displayed.");
        if (!ValidUnrealPath().IsMatch(iconPath))
            return new(generic, "Generic", "IconPath syntax is invalid. Use /Game/Folder/Asset.Asset.");

        var assetName = AssetName(iconPath);
        var local = FindLocal(assetName);
        if (local is not null) return new(local, "Icons", $"Local icon: {Path.GetFileName(local)}");

        if (!string.IsNullOrWhiteSpace(fmodelRoot) && Directory.Exists(fmodelRoot))
        {
            var exact = ResolveFModelPath(iconPath, fmodelRoot);
            var suggestion = FindSuggestion(preset, iconPath, fmodelRoot);
            if (exact is not null)
            {
                if (suggestion is not null && !suggestion.Value.Path.Equals(iconPath, StringComparison.OrdinalIgnoreCase))
                    return new(exact, "FModel", "The requested icon was found, but another exported icon better matches this preset.", suggestion.Value.Path);
                return new(exact, "FModel", "Icon resolved from the configured FModel export folder.");
            }
            if (suggestion is not null)
                return new(generic, "Generic", "The requested asset was not exported, but a likely matching icon was found.", suggestion.Value.Path);
        }
        return new(generic, "Generic", "IconPath could not be resolved; a generic icon is displayed.");
    }

    public static string Import(string sourceFile, string? preferredAssetName = null)
    {
        Directory.CreateDirectory(ImportedDirectory);
        var name = string.IsNullOrWhiteSpace(preferredAssetName)
            ? Path.GetFileName(sourceFile)
            : preferredAssetName + Path.GetExtension(sourceFile).ToLowerInvariant();
        var destination = Path.Combine(ImportedDirectory, name);
        if (File.Exists(destination))
            destination = Path.Combine(ImportedDirectory, $"{Path.GetFileNameWithoutExtension(name)}-{DateTime.Now:yyyyMMdd-HHmmss}{Path.GetExtension(name)}");
        File.Copy(sourceFile, destination, false);
        return destination;
    }

    private static string? FindLocal(string assetName)
    {
        foreach (var directory in new[] { ImportedDirectory, IconsDirectory })
        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp" })
        {
            var path = Path.Combine(directory, assetName + extension);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    private static string? ResolveFModelPath(string unrealPath, string root)
    {
        var package = unrealPath.Split('.')[0].Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        if (package.StartsWith("Game" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            package = package[(5)..];
        var directory = Path.Combine(root, Path.GetDirectoryName(package) ?? "");
        var name = Path.GetFileName(package);
        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".bmp" })
        {
            var direct = Path.Combine(directory, name + extension);
            if (File.Exists(direct)) return direct;
        }
        if (!Directory.Exists(directory)) return null;
        return Directory.EnumerateFiles(directory, name + ".*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(IsSupportedImage);
    }

    private static (string File, string Path)? FindSuggestion(PresetDocument preset, string currentPath, string root)
    {
        var package = currentPath.Split('.')[0].Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        if (package.StartsWith("Game" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) package = package[5..];
        var directory = Path.Combine(root, Path.GetDirectoryName(package) ?? "");
        if (!Directory.Exists(directory)) return null;

        var tokens = Tokenize($"{preset.TargetCharacterID} {preset.UniqueID} {preset.DisplayName}")
            .Where(x => x.Length >= 3 && x is not "costume" and not "weapon" and not "base" and not "custom" and not "dlc")
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var best = Directory.EnumerateFiles(directory, "*.*", SearchOption.TopDirectoryOnly).Where(IsSupportedImage)
            .Select(file => new { File = file, Score = tokens.Count(token => Path.GetFileNameWithoutExtension(file).Contains(token, StringComparison.OrdinalIgnoreCase)) })
            .OrderByDescending(x => x.Score).ThenBy(x => x.File.Length).FirstOrDefault();
        if (best is null || best.Score < 2) return null;
        var relative = Path.GetRelativePath(root, best.File).Replace(Path.DirectorySeparatorChar, '/');
        var asset = Path.GetFileNameWithoutExtension(best.File);
        return (best.File, $"/Game/{Path.ChangeExtension(relative, null)}.{asset}");
    }

    private static IEnumerable<string> Tokenize(string value) => TokenPattern().Matches(value).Select(x => x.Value.ToLowerInvariant());
    private static string AssetName(string unrealPath) => unrealPath[(unrealPath.LastIndexOf('/') + 1)..].Split('.')[0];
    private static bool IsSupportedImage(string path) => new[] { ".png", ".jpg", ".jpeg", ".bmp", ".webp" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    private static string GenericPath(string? type)
    {
        var name = type?.Equals("Weapon", StringComparison.OrdinalIgnoreCase) == true ? "Weapon.png" :
            type?.Equals("Custom", StringComparison.OrdinalIgnoreCase) == true ? "Custom.png" : "Costume.png";
        var path = Path.Combine(IconsDirectory, name);
        return File.Exists(path) ? path : Path.Combine(IconsDirectory, "Costume.png");
    }

    [GeneratedRegex(@"^/Game/[A-Za-z0-9_./-]+$")]
    private static partial Regex ValidUnrealPath();
    [GeneratedRegex(@"[A-Za-z0-9]+")]
    private static partial Regex TokenPattern();
}
