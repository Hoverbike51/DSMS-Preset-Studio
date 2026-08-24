using System.Text.RegularExpressions;
using DSMS.Core.Models;
using DSMS.Core.Paths;

namespace DSMS.Core.Validation;

public sealed partial class PresetValidator
{
    private static readonly HashSet<string> AcceptedTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Custom", "Costume", "Weapon", "Mounts", "NPC", "Character" };

    public ValidationReport Validate(PresetDocument preset, string? fileName = null)
    {
        var report = new ValidationReport();

        if (!string.IsNullOrWhiteSpace(fileName) && !Path.GetFileName(fileName).StartsWith("DSMS-", StringComparison.OrdinalIgnoreCase))
            report.Error("DSMS001", "FileName", "The filename must start with 'DSMS-'.");

        if (preset.Version != 3)
            report.Error("DSMS002", "Version", "The active DSMS compatibility profile requires JSON schema Version 3.");
        RequiredText(report, preset.UniqueID, "UniqueID", "DSMS003");
        RequiredText(report, preset.DisplayName, "DisplayName", "DSMS004");

        if (string.IsNullOrWhiteSpace(preset.Type) || !AcceptedTypes.Contains(preset.Type))
            report.Error("DSMS005", "Type", "Accepted values are Custom, Costume, Weapon, Mounts and NPC.");
        else if (preset.Type.Equals("Character", StringComparison.OrdinalIgnoreCase))
            report.Warning("DSMS006", "Type", "'Character' is legacy syntax; use 'Custom'.");

        var type = preset.Type?.Equals("Character", StringComparison.OrdinalIgnoreCase) == true ? "Custom" : preset.Type;
        if (string.Equals(type, "Custom", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type, "Costume", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type, "Weapon", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(preset.TargetCharacterID))
                report.Error("DSMS007", "TargetCharacterID", "This preset type requires a target character ID.");
            else if (!TargetIdRegex().IsMatch(preset.TargetCharacterID))
                report.Error("DSMS008", "TargetCharacterID", "Only letters, numbers, '_' and '-' are accepted.");
        }

        ValidateRequirements(report, preset.Requirements);
        ValidateAssetPath(report, preset.IconPath, "IconPath", required: true, allowShortIconPath: true);

        if (string.Equals(type, "Weapon", StringComparison.OrdinalIgnoreCase))
            ValidateWeapon(report, preset);
        else
            ValidateBody(report, preset, string.Equals(type, "Custom", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "Costume", StringComparison.OrdinalIgnoreCase));

        ValidateAssetPath(report, preset.SkeletonPath, "SkeletonPath");
        ValidateAssetPath(report, preset.PhysicsAssetPath, "PhysicsAssetPath");
        ValidateAssetPath(report, preset.FaceMorphPath, "FaceMorphPath");
        ValidateAssetPath(report, preset.FacePath, "FacePath");
        ValidateAssetPath(report, preset.AuxiliaryMeshPath, "AuxiliaryMeshPath");
        ValidateAssetPath(report, preset.AuxiliaryPhysicsAssetPath, "AuxiliaryPhysicsAssetPath");
        ValidateAssetPath(report, preset.LinkedBodyReplacementPath, "LinkedBodyReplacementPath");
        ValidateAnimBlueprint(report, preset.FollowerAnimBlueprintPath, "FollowerAnimBlueprintPath");
        ValidateAnimBlueprint(report, preset.PhysicsAnimBlueprintPath, "PhysicsAnimBlueprintPath");

        ValidateMaterials(report, preset.BodyMaterials, "BodyMaterials");
        ValidateMaterials(report, preset.BodyOutlineMaterials, "BodyOutlineMaterials");
        ValidateMaterials(report, preset.FaceMaterials, "FaceMaterials");
        ValidateMaterials(report, preset.AuxiliaryMaterials, "AuxiliaryMaterials");
        ValidateMaterials(report, preset.WeaponMaterials, "WeaponMaterials", allowMaterialMatch: true);
        ValidateMorphs(report, preset.BodyMorphTargets, "BodyMorphTargets");
        ValidateMorphs(report, preset.FaceMorphTargets, "FaceMorphTargets");
        ValidateStringArray(report, preset.HiddenComponentMeshMatches, "HiddenComponentMeshMatches");
        ValidateStringArray(report, preset.LinkedBodyComponentMeshMatches, "LinkedBodyComponentMeshMatches");

        if (SamePath(preset.BodyPath, preset.AuxiliaryMeshPath))
        {
            if (preset.AuxiliaryMaterials is { Count: > 0 } && preset.HiddenComponentMeshMatches is { Count: > 0 })
                report.Info("DSMS042", "AuxiliaryMeshPath", "Recognized auxiliary-outline recipe: BodyPath geometry is reused with dedicated auxiliary materials while the native component is hidden.");
            else
                report.Warning("DSMS040", "AuxiliaryMeshPath", "AuxiliaryMeshPath reuses BodyPath. Verify that DSMS is replacing an existing auxiliary component rather than spawning a duplicate.");
        }
        if (SamePath(preset.BodyPath, preset.BodyOutlinePath))
            report.Warning("DSMS041", "BodyOutlinePath", "BodyOutlinePath reuses BodyPath. This is valid only when the outline materials are designed for that geometry.");

        if (preset.ExtraFields is { Count: > 0 })
            foreach (var field in preset.ExtraFields.Keys.OrderBy(x => x))
                report.Warning("DSMS090", field, "Unknown field: verify that the current Lua runtime supports it.");

        return report;
    }

    private static void ValidateBody(ValidationReport report, PresetDocument preset, bool faceExpected)
    {
        ValidateAssetPath(report, preset.BodyPath, "BodyPath", required: true);
        ValidateAssetPath(report, preset.BodyOutlinePath, "BodyOutlinePath");
        if (faceExpected && string.IsNullOrWhiteSpace(preset.FaceMorphPath))
            report.Error("DSMS011", "FaceMorphPath", "Custom and Costume presets require the in-game face mesh to preserve facial animation and morph targets.");
        if (!string.IsNullOrWhiteSpace(preset.BodyPath) && string.IsNullOrWhiteSpace(preset.PhysicsAnimBlueprintPath))
            report.Warning("DSMS012", "PhysicsAnimBlueprintPath", "No physics Animation Blueprint is declared; secondary costume physics may be missing.");
    }

    private static void ValidateWeapon(ValidationReport report, PresetDocument preset)
    {
        var hasSingle = !string.IsNullOrWhiteSpace(preset.WeaponPath);
        var hasMultiple = preset.WeaponPaths is { Count: > 0 };
        var materialsOnly = preset.WeaponMaterialsOnly == true;

        if (!hasSingle && !hasMultiple && !materialsOnly)
            report.Error("DSMS020", "WeaponPath", "Provide WeaponPath, WeaponPaths, or set WeaponMaterialsOnly to true.");
        if (hasSingle && hasMultiple)
            report.Warning("DSMS021", "WeaponPaths", "Both WeaponPath and WeaponPaths are present; keep only the intended strategy.");
        ValidateAssetPath(report, preset.WeaponPath, "WeaponPath");
        if (materialsOnly && preset.WeaponMaterials is not { Count: > 0 })
            report.Error("DSMS022", "WeaponMaterials", "WeaponMaterialsOnly requires at least one weapon material override.");

        if (preset.WeaponPaths is null) return;
        for (var i = 0; i < preset.WeaponPaths.Count; i++)
        {
            var entry = preset.WeaponPaths[i];
            RequiredText(report, entry.ComponentMatch, $"WeaponPaths[{i}].ComponentMatch", "DSMS023");
            ValidateAssetPath(report, entry.WeaponPath, $"WeaponPaths[{i}].WeaponPath", required: true);
        }
    }

    private static void ValidateRequirements(ValidationReport report, List<string>? values)
    {
        if (values is null)
        {
            report.Info("DSMS030", "Requirements", "Missing Requirements defaults to ['None'] in the runtime.");
            return;
        }
        ValidateStringArray(report, values, "Requirements", requireOne: true);
    }

    private static void ValidateMaterials(ValidationReport report, List<MaterialOverride>? values, string field, bool allowMaterialMatch = false)
    {
        if (values is null) return;
        var selectors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < values.Count; i++)
        {
            var item = values[i];
            if (item.SlotIndex is < 0)
                report.Error("DSMS031", $"{field}[{i}].SlotIndex", "SlotIndex must be zero or greater.");
            if (item.SlotIndex is null && !(allowMaterialMatch && !string.IsNullOrWhiteSpace(item.MaterialMatch)))
                report.Error("DSMS039", $"{field}[{i}]", "SlotIndex is required unless a WeaponMaterials entry uses MaterialMatch.");
            if (!allowMaterialMatch && !string.IsNullOrWhiteSpace(item.MaterialMatch))
                report.Error("DSMS043", $"{field}[{i}].MaterialMatch", "MaterialMatch is supported only by WeaponMaterials.");
            var selector = !string.IsNullOrWhiteSpace(item.MaterialMatch)
                ? "match:" + item.MaterialMatch.Trim()
                : "slot:" + item.SlotIndex;
            if (!selectors.Add(selector))
                report.Error("DSMS032", $"{field}[{i}]", $"Duplicate material selector {selector}.");
            ValidateAssetPath(report, item.MaterialPath, $"{field}[{i}].MaterialPath", required: true);
        }
    }

    private static void ValidateMorphs(ValidationReport report, List<MorphTargetOverride>? values, string field)
    {
        if (values is null) return;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < values.Count; i++)
        {
            var item = values[i];
            RequiredText(report, item.MorphName, $"{field}[{i}].MorphName", "DSMS033");
            if (!string.IsNullOrWhiteSpace(item.MorphName) && !names.Add(item.MorphName))
                report.Error("DSMS034", $"{field}[{i}].MorphName", $"Duplicate morph target '{item.MorphName}'.");
            if (item.Value is < 0f or > 1f)
                report.Error("DSMS035", $"{field}[{i}].Value", "DSMS accepts morph target values only between 0.0 and 1.0.");
        }
    }

    private static void ValidateStringArray(ValidationReport report, List<string>? values, string field, bool requireOne = false)
    {
        if (values is null) return;
        if (requireOne && values.Count == 0)
            report.Error("DSMS036", field, "The array must contain at least one value.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < values.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(values[i]))
                report.Error("DSMS037", $"{field}[{i}]", "The value must be a non-empty string.");
            else if (!seen.Add(values[i]))
                report.Error("DSMS038", $"{field}[{i}]", $"Duplicate value '{values[i]}'.");
        }
    }

    private static void ValidateAssetPath(ValidationReport report, string? value, string field, bool required = false, bool allowShortIconPath = false)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            if (required) report.Error("DSMS010", field, $"{field} is required.");
            return;
        }
        if (allowShortIconPath && value.StartsWith("/Game/", StringComparison.Ordinal) && value.LastIndexOf('.') <= value.LastIndexOf('/'))
        {
            report.Info("DSMS014", field, "The runtime will append the icon object name automatically; a full object path is still recommended.");
            return;
        }
        if (!UnrealObjectPath.IsFullObjectPath(value))
            report.Error("DSMS013", field, "Use a full Unreal object path: /Game/.../Asset.Asset (never a Windows path).");
        else if (UnrealObjectPath.TryParse(value, out var parsed) && parsed is not null &&
                 !string.Equals(parsed.AssetName, parsed.ObjectName, StringComparison.Ordinal))
            report.Error("DSMS017", field, $"Package asset '{parsed.AssetName}' and object name '{parsed.ObjectName}' must be identical.");
    }

    private static void ValidateAnimBlueprint(ValidationReport report, string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase)) return;
        var normalized = UnrealObjectPath.CanonicalizeAnimBlueprintClassPath(value);
        if (normalized is null || !UnrealObjectPath.IsFullObjectPath(normalized))
            report.Error("DSMS015", field, "Use an Animation Blueprint path under /Game/. DSMS accepts the asset path and adds the generated class suffix _C.");
        else if (UnrealObjectPath.TryParse(normalized, out var parsed) && parsed is not null &&
                 !string.Equals(parsed.ObjectName, parsed.AssetName + "_C", StringComparison.Ordinal))
            report.Error("DSMS018", field, $"Animation Blueprint object must be '{parsed.AssetName}_C'.");
        else if (!string.Equals(value.Trim('\'', '"'), normalized, StringComparison.Ordinal))
            report.Info("DSMS016", field, $"Runtime normalized class path: {normalized}");
    }

    private static void RequiredText(ValidationReport report, string? value, string field, string code)
    {
        if (string.IsNullOrWhiteSpace(value)) report.Error(code, field, $"{field} is required.");
    }

    private static bool SamePath(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) &&
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex TargetIdRegex();
}
