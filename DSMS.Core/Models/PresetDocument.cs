using System.Text.Json;
using System.Text.Json.Serialization;

namespace DSMS.Core.Models;

public sealed class PresetDocument
{
    public int Version { get; set; }
    public string? UniqueID { get; set; }
    public string? DisplayName { get; set; }
    public string? Type { get; set; }
    public string? TargetCharacterID { get; set; }
    public List<string>? Requirements { get; set; }

    public string? BodyPath { get; set; }
    public string? SkeletonPath { get; set; }
    public string? PhysicsAssetPath { get; set; }
    public string? FollowerAnimBlueprintPath { get; set; }
    public string? PhysicsAnimBlueprintPath { get; set; }
    public bool? BodyClearMaterialOverrides { get; set; }
    public List<MaterialOverride>? BodyMaterials { get; set; }
    public List<MorphTargetOverride>? BodyMorphTargets { get; set; }

    public string? BodyOutlinePath { get; set; }
    public bool? BodyOutlineClearMaterialOverrides { get; set; }
    public List<MaterialOverride>? BodyOutlineMaterials { get; set; }

    public string? FaceMorphPath { get; set; }
    public string? FacePath { get; set; }
    public bool? FaceClearMaterialOverrides { get; set; }
    public List<MaterialOverride>? FaceMaterials { get; set; }
    public List<MorphTargetOverride>? FaceMorphTargets { get; set; }

    public string? FaceOutlinePath { get; set; }
    public bool? FaceOutlineClearMaterialOverrides { get; set; }
    public List<MaterialOverride>? FaceOutlineMaterials { get; set; }

    public string? AuxiliaryMeshPath { get; set; }
    public string? AuxiliaryPhysicsAssetPath { get; set; }
    public bool? AuxiliarySpawnOnly { get; set; }
    public bool? AuxiliaryClearMaterialOverrides { get; set; }
    public List<MaterialOverride>? AuxiliaryMaterials { get; set; }
    public List<string>? HiddenComponentMeshMatches { get; set; }

    public List<string>? LinkedBodyComponentMeshMatches { get; set; }
    public string? LinkedBodyReplacementPath { get; set; }

    public string? WeaponPath { get; set; }
    public List<WeaponMeshEntry>? WeaponPaths { get; set; }
    public bool? WeaponClearMaterialOverrides { get; set; }
    public List<MaterialOverride>? WeaponMaterials { get; set; }
    public bool? WeaponMaterialsOnly { get; set; }

    public string? IconPath { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}

public sealed class MaterialOverride
{
    public int? SlotIndex { get; set; }
    public string? MaterialMatch { get; set; }
    public string? MaterialPath { get; set; }
}

public sealed class MorphTargetOverride
{
    public string? MorphName { get; set; }
    public float Value { get; set; }
}

public sealed class WeaponMeshEntry
{
    public string? ComponentMatch { get; set; }
    public string? WeaponPath { get; set; }
}
