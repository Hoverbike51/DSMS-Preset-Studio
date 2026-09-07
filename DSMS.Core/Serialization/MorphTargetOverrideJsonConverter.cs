using System.Text.Json;
using System.Text.Json.Serialization;
using DSMS.Core.Models;

namespace DSMS.Core.Serialization;

public sealed class MorphTargetOverrideJsonConverter : JsonConverter<MorphTargetOverride>
{
    private static readonly string[] NameAliases = ["MorphName", "Name", "morph_name", "name"];
    private static readonly string[] ValueAliases = ["Value", "value"];
    private static readonly string[] DescriptionAliases = ["Description", "description"];
    private static readonly string[] MinAliases = ["Min", "MinValue", "min", "min_value"];
    private static readonly string[] MaxAliases = ["Max", "MaxValue", "max", "max_value"];
    private static readonly string[] LockedAliases = ["Locked", "locked"];
    private static readonly HashSet<string> KnownNames =
        [.. NameAliases, .. ValueAliases, .. DescriptionAliases, .. MinAliases, .. MaxAliases, .. LockedAliases];

    public override MorphTargetOverride Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("A Morph Target entry must be a JSON object.");

        var result = new MorphTargetOverride();
        if (TryGetFirst(root, NameAliases, out var name))
            result.MorphName = ReadString(name, "MorphName");
        if (!TryGetFirst(root, ValueAliases, out var value))
            throw new JsonException("Morph Target Value is required.");
        result.Value = ReadFiniteSingle(value, "Value");
        if (TryGetFirst(root, DescriptionAliases, out var description))
            result.Description = ReadString(description, "Description");
        if (TryGetFirst(root, MinAliases, out var min))
            result.Min = ReadFiniteSingle(min, "Min");
        if (TryGetFirst(root, MaxAliases, out var max))
            result.Max = ReadFiniteSingle(max, "Max");
        if (TryGetFirst(root, LockedAliases, out var locked))
            result.Locked = ReadBoolean(locked, "Locked");

        Dictionary<string, JsonElement>? extraFields = null;
        foreach (var property in root.EnumerateObject())
        {
            if (KnownNames.Contains(property.Name)) continue;
            extraFields ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            extraFields[property.Name] = property.Value.Clone();
        }
        result.ExtraFields = extraFields;
        return result;
    }

    public override void Write(Utf8JsonWriter writer, MorphTargetOverride value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.MorphName is not null) writer.WriteString("MorphName", value.MorphName);
        writer.WriteNumber("Value", value.Value);
        if (value.Description is not null) writer.WriteString("Description", value.Description);
        if (value.Min is not null) writer.WriteNumber("Min", value.Min.Value);
        if (value.Max is not null) writer.WriteNumber("Max", value.Max.Value);
        if (value.Locked is not null) writer.WriteBoolean("Locked", value.Locked.Value);
        if (value.ExtraFields is not null)
        {
            foreach (var property in value.ExtraFields)
            {
                if (KnownNames.Contains(property.Key)) continue;
                writer.WritePropertyName(property.Key);
                property.Value.WriteTo(writer);
            }
        }
        writer.WriteEndObject();
    }

    private static bool TryGetFirst(JsonElement root, IEnumerable<string> names, out JsonElement value)
    {
        foreach (var name in names)
            if (root.TryGetProperty(name, out value))
                return true;
        value = default;
        return false;
    }

    private static string ReadString(JsonElement value, string propertyName) =>
        value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new JsonException($"Morph Target {propertyName} must be a string.");

    private static bool ReadBoolean(JsonElement value, string propertyName) => value.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw new JsonException($"Morph Target {propertyName} must be Boolean.")
    };

    private static float ReadFiniteSingle(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetSingle(out var number) || !float.IsFinite(number))
            throw new JsonException($"Morph Target {propertyName} must be a finite number.");
        return number;
    }
}
