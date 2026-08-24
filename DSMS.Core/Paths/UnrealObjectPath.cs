namespace DSMS.Core.Paths;

public static class UnrealObjectPath
{
    public sealed record ParsedPath(string PackagePath, string ObjectName, string AssetName);

    public static bool IsFullObjectPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("/Game/", StringComparison.Ordinal))
            return false;

        var slash = value.LastIndexOf('/');
        var dot = value.LastIndexOf('.');
        return dot > slash + 1 && dot < value.Length - 1;
    }

    public static bool TryParse(string? value, out ParsedPath? parsed)
    {
        parsed = null;
        if (!IsFullObjectPath(value) || value!.Contains('\\')) return false;
        var slash = value.LastIndexOf('/');
        var dot = value.LastIndexOf('.');
        var assetName = value[(slash + 1)..dot];
        var objectName = value[(dot + 1)..];
        if (assetName.Length == 0 || objectName.Length == 0) return false;
        parsed = new(value[..dot], objectName, assetName);
        return true;
    }

    public static string? CanonicalizeAssetPath(string? value, bool appendObjectName = false)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase))
            return null;

        value = value.Trim().Trim('\'', '"').Replace('\\', '/');
        if (!value.StartsWith("/Game/", StringComparison.Ordinal))
            return value;

        if (appendObjectName && value.LastIndexOf('.') <= value.LastIndexOf('/'))
        {
            var objectName = value[(value.LastIndexOf('/') + 1)..];
            value += "." + objectName;
        }

        return value;
    }

    public static string? CanonicalizeAnimBlueprintClassPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Equals("None", StringComparison.OrdinalIgnoreCase))
            return null;

        value = value.Trim();
        foreach (var prefix in new[] { "AnimBlueprintGeneratedClass", "BlueprintGeneratedClass", "Class" })
        {
            if (value.StartsWith(prefix, StringComparison.Ordinal))
                value = value[prefix.Length..].Trim();
        }

        value = value.Trim('\'', '"');
        if (!value.StartsWith("/Game/", StringComparison.Ordinal))
            return value;

        var slash = value.LastIndexOf('/');
        var dot = value.LastIndexOf('.');
        var package = dot > slash ? value[..dot] : value;
        var objectName = dot > slash ? value[(dot + 1)..] : value[(slash + 1)..];
        if (!objectName.EndsWith("_C", StringComparison.Ordinal))
            objectName += "_C";
        return package + "." + objectName;
    }
}
