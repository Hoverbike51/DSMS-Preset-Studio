using DSMS.Core.Catalog;
using DSMS.Core.Models;
using DSMS.Core.Paths;

namespace DSMS.Core.Repair;

public sealed record RepairChange(string Field, string Message);

public sealed class PresetRepairer(CharacterCatalog characterCatalog)
{
    public IReadOnlyList<RepairChange> Repair(PresetDocument preset)
    {
        var changes = new List<RepairChange>();

        if (preset.Version != 3)
        {
            preset.Version = 3;
            changes.Add(new("Version", "Set the active JSON schema version to 3."));
        }

        preset.UniqueID = Trim(preset.UniqueID, "UniqueID", changes);
        preset.DisplayName = Trim(preset.DisplayName, "DisplayName", changes);
        preset.PresetName = Trim(preset.PresetName, "PresetName", changes);
        preset.PresetVariant = Trim(preset.PresetVariant, "PresetVariant", changes);
        preset.Type = Trim(preset.Type, "Type", changes);
        if (preset.Type?.Equals("Character", StringComparison.OrdinalIgnoreCase) == true)
        {
            preset.Type = "Custom";
            changes.Add(new("Type", "Replaced the legacy Character type with Custom."));
        }

        preset.TargetCharacterID = Trim(preset.TargetCharacterID, "TargetCharacterID", changes);
        var character = characterCatalog.Find(preset.TargetCharacterID);
        if (character is not null && !string.Equals(preset.TargetCharacterID, character.InternalId, StringComparison.Ordinal))
        {
            var previous = preset.TargetCharacterID;
            preset.TargetCharacterID = character.InternalId;
            changes.Add(new("TargetCharacterID", $"Resolved '{previous}' to the canonical character ID '{character.InternalId}'."));
        }

        preset.Requirements = NormalizeStrings(preset.Requirements, "Requirements", changes, defaultNone: true);
        preset.HiddenComponentMeshMatches = NormalizeStrings(preset.HiddenComponentMeshMatches, "HiddenComponentMeshMatches", changes);
        preset.LinkedBodyComponentMeshMatches = NormalizeStrings(preset.LinkedBodyComponentMeshMatches, "LinkedBodyComponentMeshMatches", changes);

        NormalizeAssetPath(preset.BodyPath, value => preset.BodyPath = value, "BodyPath", changes);
        NormalizeAssetPath(preset.SkeletonPath, value => preset.SkeletonPath = value, "SkeletonPath", changes);
        NormalizeAssetPath(preset.PhysicsAssetPath, value => preset.PhysicsAssetPath = value, "PhysicsAssetPath", changes);
        NormalizeAssetPath(preset.BodyOutlinePath, value => preset.BodyOutlinePath = value, "BodyOutlinePath", changes);
        NormalizeAssetPath(preset.FaceMorphPath, value => preset.FaceMorphPath = value, "FaceMorphPath", changes);
        NormalizeAssetPath(preset.FacePath, value => preset.FacePath = value, "FacePath", changes);
        NormalizeAssetPath(preset.FaceOutlinePath, value => preset.FaceOutlinePath = value, "FaceOutlinePath", changes);
        NormalizeAssetPath(preset.AuxiliaryMeshPath, value => preset.AuxiliaryMeshPath = value, "AuxiliaryMeshPath", changes);
        NormalizeAssetPath(preset.AuxiliaryPhysicsAssetPath, value => preset.AuxiliaryPhysicsAssetPath = value, "AuxiliaryPhysicsAssetPath", changes);
        NormalizeAssetPath(preset.LinkedBodyReplacementPath, value => preset.LinkedBodyReplacementPath = value, "LinkedBodyReplacementPath", changes);
        NormalizeAssetPath(preset.WeaponPath, value => preset.WeaponPath = value, "WeaponPath", changes);
        preset.NormalizeIconPathForType();
        var iconField = preset.IconFieldName;
        NormalizeAssetPath(preset.ActiveIconPath, value => preset.ActiveIconPath = value, iconField, changes);
        NormalizeAnimPath(preset.FollowerAnimBlueprintPath, value => preset.FollowerAnimBlueprintPath = value, "FollowerAnimBlueprintPath", changes);
        NormalizeAnimPath(preset.PhysicsAnimBlueprintPath, value => preset.PhysicsAnimBlueprintPath = value, "PhysicsAnimBlueprintPath", changes);
        NormalizeAnimPath(preset.WeaponActionSourceBlueprintPath,
            value => preset.WeaponActionSourceBlueprintPath = value,
            "WeaponActionSourceBlueprintPath", changes);

        NormalizeMaterials(preset.BodyMaterials, "BodyMaterials", changes);
        NormalizeMaterials(preset.BodyOutlineMaterials, "BodyOutlineMaterials", changes);
        NormalizeMaterials(preset.FaceMaterials, "FaceMaterials", changes);
        NormalizeMaterials(preset.FaceOutlineMaterials, "FaceOutlineMaterials", changes);
        NormalizeMaterials(preset.AuxiliaryMaterials, "AuxiliaryMaterials", changes);
        NormalizeMaterials(preset.WeaponMaterials, "WeaponMaterials", changes);
        NormalizeMorphs(preset.BodyMorphTargets, "BodyMorphTargets", changes);
        NormalizeMorphs(preset.FaceMorphTargets, "FaceMorphTargets", changes);

        if (preset.WeaponPaths is not null)
        {
            for (var i = 0; i < preset.WeaponPaths.Count; i++)
            {
                var index = i;
                preset.WeaponPaths[i].ComponentMatch = Trim(preset.WeaponPaths[i].ComponentMatch, $"WeaponPaths[{i}].ComponentMatch", changes);
                NormalizeAssetPath(preset.WeaponPaths[i].WeaponPath,
                    value => preset.WeaponPaths[index].WeaponPath = value,
                    $"WeaponPaths[{i}].WeaponPath", changes);
            }
        }

        return changes;
    }

    private static string? Trim(string? value, string field, ICollection<RepairChange> changes)
    {
        if (value is null) return null;
        var normalized = value.Trim();
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
            changes.Add(new(field, "Removed leading or trailing whitespace."));
        return normalized.Length == 0 ? null : normalized;
    }

    private static List<string>? NormalizeStrings(List<string>? values, string field,
        ICollection<RepairChange> changes, bool defaultNone = false)
    {
        if (values is null || values.Count == 0)
        {
            if (!defaultNone) return values;
            changes.Add(new(field, "Added the runtime default value 'None'."));
            return ["None"];
        }

        var normalized = values.Select(x => x?.Trim() ?? "")
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!values.SequenceEqual(normalized, StringComparer.Ordinal))
            changes.Add(new(field, "Removed empty or duplicate values and normalized whitespace."));
        if (normalized.Count == 0 && defaultNone) normalized.Add("None");
        return normalized;
    }

    private static void NormalizeAssetPath(string? current, Action<string?> assign, string field,
        ICollection<RepairChange> changes)
    {
        var normalized = UnrealObjectPath.CanonicalizeAssetPath(current, appendObjectName: true);
        if (!string.Equals(current, normalized, StringComparison.Ordinal))
        {
            assign(normalized);
            changes.Add(new(field, $"Normalized the Unreal object path to '{normalized ?? "None"}'."));
        }
    }

    private static void NormalizeAnimPath(string? current, Action<string?> assign, string field,
        ICollection<RepairChange> changes)
    {
        var normalized = UnrealObjectPath.CanonicalizeAnimBlueprintClassPath(current);
        if (!string.Equals(current, normalized, StringComparison.Ordinal))
        {
            assign(normalized);
            changes.Add(new(field, $"Normalized the Animation Blueprint class path to '{normalized ?? "None"}'."));
        }
    }

    private static void NormalizeMaterials(List<MaterialOverride>? materials, string field,
        ICollection<RepairChange> changes)
    {
        if (materials is null) return;
        for (var i = 0; i < materials.Count; i++)
        {
            var index = i;
            materials[i].MaterialMatch = Trim(materials[i].MaterialMatch, $"{field}[{i}].MaterialMatch", changes);
            NormalizeAssetPath(materials[i].MaterialPath,
                value => materials[index].MaterialPath = value,
                $"{field}[{i}].MaterialPath", changes);
        }

        // Preserve authored ordering and extension data. Duplicate selectors are reported by validation
        // and must be resolved explicitly instead of silently dropping a future nested property.
    }

    private static void NormalizeMorphs(List<MorphTargetOverride>? morphs, string field,
        ICollection<RepairChange> changes)
    {
        if (morphs is null) return;
        for (var i = 0; i < morphs.Count; i++)
        {
            var morph = morphs[i];
            morph.MorphName = Trim(morph.MorphName, $"{field}[{i}].MorphName", changes);
            if (morph.Description is null) continue;
            var description = morph.Description.Trim();
            if (!string.Equals(description, morph.Description, StringComparison.Ordinal))
                changes.Add(new($"{field}[{i}].Description", "Removed leading or trailing whitespace."));
            morph.Description = description;
        }
    }
}
