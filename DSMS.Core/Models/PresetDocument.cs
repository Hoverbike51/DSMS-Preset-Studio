using System.Text.Json;
using System.Text.Json.Serialization;

namespace DSMS.Core.Models;

public sealed class PresetDocument
{
    private string? _presetName;
    private string? _presetVariant;
    private string? _characterIconPath;
    private string? _weaponIconPath;
    private string? _weaponActionSourceBlueprintPath;
    private string? _legacyIconPath;
    private int _presetNamePriority = int.MaxValue;
    private int _presetVariantPriority = int.MaxValue;
    private int _characterIconPathPriority = int.MaxValue;
    private int _weaponIconPathPriority = int.MaxValue;
    private int _weaponActionSourceBlueprintPathPriority = int.MaxValue;
    private int _legacyIconPathPriority = int.MaxValue;

    public int Version { get; set; }
    public string? UniqueID { get; set; }
    public string? DisplayName { get; set; }
    public string? PresetName
    {
        get => _presetName;
        set => SetByPriority(ref _presetName, ref _presetNamePriority, value, 0);
    }
    public string? PresetVariant
    {
        get => _presetVariant;
        set => SetByPriority(ref _presetVariant, ref _presetVariantPriority, value, 0);
    }
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

    public string? WeaponActionSourceBlueprintPath
    {
        get => _weaponActionSourceBlueprintPath;
        set => SetByPriority(ref _weaponActionSourceBlueprintPath,
            ref _weaponActionSourceBlueprintPathPriority, value, 0);
    }

    public string? CharacterIconPath
    {
        get => _characterIconPath;
        set => SetByPriority(ref _characterIconPath, ref _characterIconPathPriority, value, 0);
    }

    public string? WeaponIconPath
    {
        get => _weaponIconPath;
        set => SetByPriority(ref _weaponIconPath, ref _weaponIconPathPriority, value, 0);
    }

    [JsonIgnore]
    public bool UsesWeaponIcon => Type?.Equals("Weapon", StringComparison.OrdinalIgnoreCase) == true;

    [JsonIgnore]
    public string IconFieldName => UsesWeaponIcon ? "WeaponIconPath" : "CharacterIconPath";

    [JsonIgnore]
    public string? ActiveIconPath
    {
        get => UsesWeaponIcon ? WeaponIconPath : CharacterIconPath;
        set
        {
            if (UsesWeaponIcon) WeaponIconPath = value;
            else CharacterIconPath = value;
        }
    }

    [JsonPropertyName("preset_name")]
    public string? LegacyPresetName
    {
        get => null;
        set => SetByPriority(ref _presetName, ref _presetNamePriority, value, 1);
    }

    [JsonPropertyName("preset_variant")]
    public string? LegacyPresetVariant
    {
        get => null;
        set => SetByPriority(ref _presetVariant, ref _presetVariantPriority, value, 1);
    }

    [JsonPropertyName("character_icon_path")]
    public string? LegacyCharacterIconPath
    {
        get => null;
        set => SetByPriority(ref _characterIconPath, ref _characterIconPathPriority, value, 1);
    }

    [JsonPropertyName("IconPath")]
    public string? LegacyIconPath
    {
        get => null;
        set => SetByPriority(ref _legacyIconPath, ref _legacyIconPathPriority, value, 2);
    }

    [JsonPropertyName("IconPaths")]
    public string? LegacyIconPaths
    {
        get => null;
        set => SetByPriority(ref _legacyIconPath, ref _legacyIconPathPriority, value, 3);
    }

    [JsonPropertyName("icon_path")]
    public string? LegacySnakeCaseIconPath
    {
        get => null;
        set => SetByPriority(ref _legacyIconPath, ref _legacyIconPathPriority, value, 4);
    }

    [JsonPropertyName("weapon_icon_path")]
    public string? LegacyWeaponIconPath
    {
        get => null;
        set => SetByPriority(ref _weaponIconPath, ref _weaponIconPathPriority, value, 1);
    }

    [JsonPropertyName("weapon_action_source_blueprint_path")]
    public string? LegacyWeaponActionSourceBlueprintPath
    {
        get => null;
        set => SetByPriority(ref _weaponActionSourceBlueprintPath,
            ref _weaponActionSourceBlueprintPathPriority, value, 1);
    }

    public void NormalizeIconPathForType()
    {
        if (UsesWeaponIcon)
        {
            _weaponIconPath ??= _characterIconPath ?? _legacyIconPath;
            _characterIconPath = null;
            _characterIconPathPriority = int.MaxValue;
        }
        else
        {
            _characterIconPath ??= _legacyIconPath;
            _weaponIconPath = null;
            _weaponIconPathPriority = int.MaxValue;
        }
        _legacyIconPath = null;
        _legacyIconPathPriority = int.MaxValue;
    }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }

    private static void SetByPriority(ref string? target, ref int currentPriority, string? value, int priority)
    {
        if (priority > currentPriority) return;
        target = value;
        currentPriority = priority;
    }
}

public sealed class MaterialOverride
{
    public int? SlotIndex { get; set; }
    public string? MaterialMatch { get; set; }
    public string? MaterialPath { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}

public sealed class MorphTargetOverride
{
    public string? MorphName { get; set; }
    public float Value { get; set; }
    public string? Description { get; set; }
    public float? Min { get; set; }
    public float? Max { get; set; }
    public bool? Locked { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}

public sealed class WeaponMeshEntry
{
    public string? ComponentMatch { get; set; }
    public string? WeaponPath { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}
