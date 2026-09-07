using System.Text.Json;
using DSMS.Core.Models;

namespace DSMS.Core.Serialization;

public static class PresetSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new MorphTargetOverrideJsonConverter() }
    };

    public static PresetDocument Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<PresetDocument>(json, Options)
            ?? throw new JsonException("The JSON document is empty.");
        document.NormalizeIconPathForType();
        return document;
    }

    public static string Serialize(PresetDocument document)
    {
        document.NormalizeIconPathForType();
        return JsonSerializer.Serialize(document, Options) + Environment.NewLine;
    }
}
