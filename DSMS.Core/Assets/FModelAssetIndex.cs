using DSMS.Core.Paths;

namespace DSMS.Core.Assets;

public enum AssetLookupKind { Exact, CaseMismatch, Missing }

public sealed record AssetLookup(
    AssetLookupKind Kind,
    string RequestedPath,
    string? CanonicalPath = null,
    string? SuggestedPath = null);

public sealed class FModelAssetIndex
{
    private static readonly HashSet<string> ExportExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".json", ".uemodel", ".png", ".jpg", ".jpeg", ".bmp", ".webp", ".uasset"
    };

    private readonly Dictionary<string, string> _packages;
    private readonly Dictionary<string, List<string>> _directories;

    private FModelAssetIndex(string root, Dictionary<string, string> packages)
    {
        Root = root;
        _packages = packages;
        _directories = packages.Values.GroupBy(DirectoryOf, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    public string Root { get; }
    public int Count => _packages.Count;

    public static FModelAssetIndex Build(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            throw new DirectoryNotFoundException("Select an existing FModel DS/Content export folder first.");

        root = Path.GetFullPath(root.Trim());
        var packages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            if (!ExportExtensions.Contains(Path.GetExtension(file))) continue;
            var relative = Path.GetRelativePath(root, file);
            var package = "/Game/" + Path.ChangeExtension(relative, null)!.Replace('\\', '/');
            packages.TryAdd(package, package);
        }
        return new(root, packages);
    }

    public AssetLookup Lookup(string objectPath, bool generatedClass = false)
    {
        if (!UnrealObjectPath.TryParse(objectPath, out var parsed) || parsed is null)
            return new(AssetLookupKind.Missing, objectPath);

        if (_packages.TryGetValue(parsed.PackagePath, out var canonicalPackage))
        {
            var canonicalObject = AssetObjectPath(canonicalPackage, generatedClass);
            return new(string.Equals(objectPath, canonicalObject, StringComparison.Ordinal)
                    ? AssetLookupKind.Exact : AssetLookupKind.CaseMismatch,
                objectPath, canonicalObject, canonicalObject);
        }

        var directory = DirectoryOf(parsed.PackagePath);
        var requestedName = LeafOf(parsed.PackagePath);
        string? suggestion = null;
        if (_directories.TryGetValue(directory, out var candidates))
        {
            var best = candidates.Select(path => new { Path = path, Distance = Distance(requestedName, LeafOf(path)) })
                .OrderBy(x => x.Distance).ThenBy(x => x.Path.Length).FirstOrDefault();
            if (best is not null && best.Distance <= Math.Max(2, requestedName.Length / 4))
                suggestion = AssetObjectPath(best.Path, generatedClass);
        }
        return new(AssetLookupKind.Missing, objectPath, SuggestedPath: suggestion);
    }

    private static string AssetObjectPath(string package, bool generatedClass)
    {
        var name = LeafOf(package) + (generatedClass ? "_C" : "");
        return package + "." + name;
    }

    private static string DirectoryOf(string package) => package[..package.LastIndexOf('/')];
    private static string LeafOf(string package) => package[(package.LastIndexOf('/') + 1)..];

    private static int Distance(string left, string right)
    {
        left = left.ToLowerInvariant(); right = right.ToLowerInvariant();
        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        for (var i = 1; i <= left.Length; i++)
        {
            var current = new int[right.Length + 1]; current[0] = i;
            for (var j = 1; j <= right.Length; j++)
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1));
            previous = current;
        }
        return previous[right.Length];
    }
}
