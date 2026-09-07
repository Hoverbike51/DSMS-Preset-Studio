using System.Text.RegularExpressions;
using DSMS.Core.Models;

namespace DSMS.Core.Presentation;

public sealed record PresetPresentationValues(string Name, string? Variant);

public static partial class PresetPresentation
{
    public static PresetPresentationValues Resolve(PresetDocument preset)
    {
        var name = TrimOptional(preset.PresetName);
        if (name is null)
        {
            name = RemoveBracketedSegments(preset.DisplayName);
            if (name.Length == 0) name = TrimOptional(preset.UniqueID) ?? "";
        }
        return new(name, TrimOptional(preset.PresetVariant));
    }

    private static string RemoveBracketedSegments(string? value)
    {
        var result = value ?? "";
        string previous;
        do
        {
            previous = result;
            result = BracketedSegment().Replace(result, "");
        } while (!string.Equals(previous, result, StringComparison.Ordinal));
        return Whitespace().Replace(result, " ").Trim();
    }

    private static string? TrimOptional(string? value)
    {
        if (value is null) return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    [GeneratedRegex(@"\s*\[[^\[\]]*\]")]
    private static partial Regex BracketedSegment();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
