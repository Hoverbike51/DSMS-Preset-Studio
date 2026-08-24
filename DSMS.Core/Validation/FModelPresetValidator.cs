using DSMS.Core.Assets;
using DSMS.Core.Models;

namespace DSMS.Core.Validation;

public sealed class FModelPresetValidator(FModelAssetIndex index)
{
    public ValidationReport Validate(PresetDocument preset)
    {
        var report = new ValidationReport();
        foreach (var path in Paths(preset)) Verify(report, path.Field, path.Value, path.GeneratedClass);
        return report;
    }

    private void Verify(ValidationReport report, string field, string? value, bool generatedClass = false)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase)) return;
        if (!value.StartsWith("/Game/", StringComparison.Ordinal) || value.LastIndexOf('.') <= value.LastIndexOf('/')) return;
        var result = index.Lookup(value, generatedClass);
        if (result.Kind == AssetLookupKind.Exact) return;
        if (result.Kind == AssetLookupKind.CaseMismatch)
        {
            report.Warning("DSMS110", field, $"FModel found this asset with different spelling or casing. Suggested path: {result.SuggestedPath}");
            return;
        }
        var custom = value.StartsWith("/Game/MODS/", StringComparison.OrdinalIgnoreCase);
        var suggestion = result.SuggestedPath is null ? "" : $" Likely match: {result.SuggestedPath}";
        if (custom)
            report.Info("DSMS112", field, $"Custom asset is not present in the current FModel index, so it remains unverified.{suggestion}");
        else
            report.Warning("DSMS111", field, $"Asset was not found in the current FModel export index.{suggestion}");
    }

    private static IEnumerable<(string Field, string? Value, bool GeneratedClass)> Paths(PresetDocument p)
    {
        yield return ("BodyPath", p.BodyPath, false); yield return ("SkeletonPath", p.SkeletonPath, false);
        yield return ("PhysicsAssetPath", p.PhysicsAssetPath, false); yield return ("FollowerAnimBlueprintPath", p.FollowerAnimBlueprintPath, true);
        yield return ("PhysicsAnimBlueprintPath", p.PhysicsAnimBlueprintPath, true); yield return ("BodyOutlinePath", p.BodyOutlinePath, false);
        yield return ("FaceMorphPath", p.FaceMorphPath, false); yield return ("FacePath", p.FacePath, false);
        yield return ("AuxiliaryMeshPath", p.AuxiliaryMeshPath, false); yield return ("AuxiliaryPhysicsAssetPath", p.AuxiliaryPhysicsAssetPath, false);
        yield return ("LinkedBodyReplacementPath", p.LinkedBodyReplacementPath, false); yield return ("WeaponPath", p.WeaponPath, false);
        yield return ("IconPath", p.IconPath, false);
        if (p.WeaponPaths is not null) for (var i = 0; i < p.WeaponPaths.Count; i++) yield return ($"WeaponPaths[{i}].WeaponPath", p.WeaponPaths[i].WeaponPath, false);
        foreach (var item in Materials(p)) yield return item;
    }

    private static IEnumerable<(string Field, string? Value, bool GeneratedClass)> Materials(PresetDocument p)
    {
        foreach (var group in new[] { ("BodyMaterials", p.BodyMaterials), ("BodyOutlineMaterials", p.BodyOutlineMaterials),
                     ("FaceMaterials", p.FaceMaterials), ("AuxiliaryMaterials", p.AuxiliaryMaterials), ("WeaponMaterials", p.WeaponMaterials) })
            if (group.Item2 is not null) for (var i = 0; i < group.Item2.Count; i++) yield return ($"{group.Item1}[{i}].MaterialPath", group.Item2[i].MaterialPath, false);
    }
}
