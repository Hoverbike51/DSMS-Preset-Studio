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
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static PresetDocument Deserialize(string json) =>
        JsonSerializer.Deserialize<PresetDocument>(json, Options)
        ?? throw new JsonException("The JSON document is empty.");

    public static string Serialize(PresetDocument document) =>
        JsonSerializer.Serialize(document, Options) + Environment.NewLine;
}
