using DSMS.Core.Assets;
using DSMS.Core.Catalog;
using DSMS.Core.Models;
using DSMS.Core.Presentation;
using DSMS.Core.Repair;
using DSMS.Core.Repository;
using DSMS.Core.Serialization;
using DSMS.Core.Validation;
using System.Text.Json;

var failures = new List<string>();
var validator = new PresetValidator();
void Expect(string name, bool condition) { Console.WriteLine($"[{(condition ? "PASS" : "FAIL")}] {name}"); if (!condition) failures.Add(name); }
string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
bool ThrowsJson(string json) { try { PresetSerializer.Deserialize(json); return false; } catch (JsonException) { return true; } }
JsonElement JsonValue(string json) { using var document = JsonDocument.Parse(json); return document.RootElement.Clone(); }
bool JsonSemanticallyEqual(string leftJson, string rightJson)
{
    using var leftDocument = JsonDocument.Parse(leftJson);
    using var rightDocument = JsonDocument.Parse(rightJson);
    return JsonElementsSemanticallyEqual(leftDocument.RootElement, rightDocument.RootElement);
}
bool JsonElementsSemanticallyEqual(JsonElement left, JsonElement right)
{
    if (left.ValueKind != right.ValueKind)
        return left.ValueKind == JsonValueKind.Number && right.ValueKind == JsonValueKind.Number &&
               left.GetDouble().Equals(right.GetDouble());

    return left.ValueKind switch
    {
        JsonValueKind.Object =>
            left.EnumerateObject().Count() == right.EnumerateObject().Count() &&
            left.EnumerateObject().All(property =>
                right.TryGetProperty(property.Name, out var rightValue) &&
                JsonElementsSemanticallyEqual(property.Value, rightValue)),
        JsonValueKind.Array =>
            left.GetArrayLength() == right.GetArrayLength() &&
            left.EnumerateArray().Zip(right.EnumerateArray()).All(pair => JsonElementsSemanticallyEqual(pair.First, pair.Second)),
        JsonValueKind.String => left.GetString() == right.GetString(),
        JsonValueKind.Number => left.GetDouble().Equals(right.GetDouble()),
        JsonValueKind.True or JsonValueKind.False => left.GetBoolean() == right.GetBoolean(),
        JsonValueKind.Null or JsonValueKind.Undefined => true,
        _ => left.GetRawText() == right.GetRawText()
    };
}
PresetDocument ValidCostume() => new()
{
    Version = 3, UniqueID = "test_astria_costume", DisplayName = "Astria - Test [Costume Custom]",
    Type = "Costume", TargetCharacterID = "Astria", Requirements = ["None"],
    BodyPath = "/Game/mods/Test/Mesh/TestBody.TestBody",
    PhysicsAnimBlueprintPath = "/Game/Test/DsABP_Test_Physics.DsABP_Test_Physics_C",
    FaceMorphPath = "/Game/Test/Test_ingame_face.Test_ingame_face", CharacterIconPath = "/Game/Test/Icon.Icon"
};

Expect("valid costume has no errors", validator.Validate(ValidCostume(), "DSMS-Test.json").ErrorCount == 0);
Expect("filename prefix is mandatory", validator.Validate(ValidCostume(), "Test.json").Issues.Any(x => x.Code == "DSMS001"));
var mismatch = ValidCostume(); mismatch.BodyPath = "/Game/mods/Test/Mesh/Ekko_Custom_Body.Eko_Custom_Brody";
Expect("package/object spelling mismatch is rejected", validator.Validate(mismatch, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS017"));
var duplicateSlots = ValidCostume(); duplicateSlots.BodyMaterials = [new() { SlotIndex = 0, MaterialPath = "/Game/Test/MI_A.MI_A" }, new() { SlotIndex = 0, MaterialPath = "/Game/Test/MI_B.MI_B" }];
Expect("duplicate material slots are rejected", validator.Validate(duplicateSlots, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS032"));

var faceOutline = ValidCostume();
faceOutline.FaceOutlinePath = "/Game/Test/Test_face_outline.Test_face_outline";
faceOutline.FaceOutlineClearMaterialOverrides = true;
faceOutline.FaceOutlineMaterials = [new() { SlotIndex = 0, MaterialPath = "/Game/Test/MI_FaceOutline.MI_FaceOutline" }];
var serializedFaceOutline = PresetSerializer.Serialize(faceOutline);
var parsedFaceOutline = PresetSerializer.Deserialize(serializedFaceOutline);
Expect("Face Outline fields survive JSON serialization", parsedFaceOutline.FaceOutlinePath == faceOutline.FaceOutlinePath &&
    parsedFaceOutline.FaceOutlineClearMaterialOverrides == true && parsedFaceOutline.FaceOutlineMaterials is { Count: 1 });
Expect("Face Outline fields are accepted by validation", validator.Validate(parsedFaceOutline, "DSMS-Test.json").Issues.All(x => x.Field is not "FaceOutlinePath" and not "FaceOutlineMaterials"));

var canonical08Json = File.ReadAllText(Fixture("dsms-0.8-morph-canonical.json"));
var canonical08 = PresetSerializer.Deserialize(canonical08Json);
var canonical08RoundTrip = PresetSerializer.Deserialize(PresetSerializer.Serialize(canonical08));
var canonicalBodyMorph = canonical08RoundTrip.BodyMorphTargets![0];
var canonicalFaceMorph = canonical08RoundTrip.FaceMorphTargets![0];
Expect("DSMS 0.8 canonical morph metadata survives round trip",
    canonicalBodyMorph.MorphName == "body_extended" && canonicalBodyMorph.Value == 2.25f &&
    canonicalBodyMorph.Description == "Extended body range." && canonicalBodyMorph.Min == -1.5f &&
    canonicalBodyMorph.Max == 3f && canonicalBodyMorph.Locked == false);
Expect("DSMS 0.8 presentation metadata survives round trip",
    canonical08RoundTrip.PresetName == "Canonical fixture" && canonical08RoundTrip.PresetVariant == "Extended morph");
Expect("omitted optional morph metadata remains omitted",
    canonicalFaceMorph.Description is null && canonicalFaceMorph.Min is null && canonicalFaceMorph.Max is null && canonicalFaceMorph.Locked is null);
Expect("unknown top-level and morph properties survive round trip",
    canonical08RoundTrip.ExtraFields?.ContainsKey("FuturePresetMetadata") == true &&
    canonicalBodyMorph.ExtraFields?.ContainsKey("FutureMorphMetadata") == true);
Expect("extended negative and above-one morph range is valid",
    validator.Validate(canonical08RoundTrip, "DSMS-Test.json").ErrorCount == 0);

var aliases08Json = File.ReadAllText(Fixture("dsms-0.8-morph-aliases.json"));
var aliases08 = PresetSerializer.Deserialize(aliases08Json);
var aliasesSerialized = PresetSerializer.Serialize(aliases08);
using (var aliasesDocument = JsonDocument.Parse(aliasesSerialized))
{
    var morphJson = aliasesDocument.RootElement.GetProperty("BodyMorphTargets")[0];
    Expect("DSMS 0.8 aliases normalize to one canonical representation",
        morphJson.TryGetProperty("MorphName", out _) && morphJson.TryGetProperty("Value", out _) &&
        morphJson.TryGetProperty("Description", out _) && morphJson.TryGetProperty("Min", out _) &&
        morphJson.TryGetProperty("Max", out _) && morphJson.TryGetProperty("Locked", out _) &&
        !morphJson.TryGetProperty("Name", out _) && !morphJson.TryGetProperty("MinValue", out _) &&
        !morphJson.TryGetProperty("max_value", out _) && !morphJson.TryGetProperty("locked", out _));
}
var aliasesRoundTrip = PresetSerializer.Deserialize(aliasesSerialized);
using (var aliasesDocument = JsonDocument.Parse(aliasesSerialized))
{
    var root = aliasesDocument.RootElement;
    var propertyNames = root.EnumerateObject().Select(property => property.Name).ToArray();
    Expect("preset aliases normalize to canonical output",
        root.GetProperty("PresetName").GetString() == "Alias fixture" &&
        root.GetProperty("PresetVariant").GetString() == "Legacy field import" &&
        root.GetProperty("WeaponIconPath").GetString() == "/Game/Test/Icon.Icon" &&
        !root.TryGetProperty("CharacterIconPath", out _) &&
        !root.TryGetProperty("preset_name", out _) && !root.TryGetProperty("preset_variant", out _) &&
        !root.TryGetProperty("character_icon_path", out _) && !root.TryGetProperty("IconPath", out _) &&
        !root.TryGetProperty("IconPaths", out _) && !root.TryGetProperty("icon_path", out _));
    Expect("canonical identity output order follows the DSMS 0.8 contract",
        Array.IndexOf(propertyNames, "Version") < Array.IndexOf(propertyNames, "UniqueID") &&
        Array.IndexOf(propertyNames, "UniqueID") < Array.IndexOf(propertyNames, "DisplayName") &&
        Array.IndexOf(propertyNames, "DisplayName") < Array.IndexOf(propertyNames, "PresetName") &&
        Array.IndexOf(propertyNames, "PresetName") < Array.IndexOf(propertyNames, "PresetVariant") &&
        Array.IndexOf(propertyNames, "PresetVariant") < Array.IndexOf(propertyNames, "Type"));
}
Expect("DSMS 0.8 alias fixture validates after canonicalization",
    validator.Validate(aliasesRoundTrip, "DSMS-Test.json").ErrorCount == 0);
Expect("unknown material, weapon and morph properties survive round trip",
    aliasesRoundTrip.WeaponMaterials![0].ExtraFields?.ContainsKey("FutureMaterialMetadata") == true &&
    aliasesRoundTrip.WeaponPaths![0].ExtraFields?.ContainsKey("FutureWeaponMetadata") == true &&
    aliasesRoundTrip.BodyMorphTargets![0].ExtraFields?.ContainsKey("FutureMorphMetadata") == true);

foreach (var alias in new[] { "character_icon_path", "IconPath", "IconPaths", "icon_path" })
{
    var imported = PresetSerializer.Deserialize($$"""{"Version":3,"UniqueID":"alias","DisplayName":"Alias","Type":"Custom","TargetCharacterID":"Astria","Requirements":["None"],"{{alias}}":"/Game/Test/Icon"}""");
    Expect($"{alias} imports as CharacterIconPath", imported.CharacterIconPath == "/Game/Test/Icon");
}
var iconPrecedence = PresetSerializer.Deserialize("""{"Version":3,"UniqueID":"precedence","DisplayName":"Precedence","Type":"Custom","TargetCharacterID":"Astria","Requirements":["None"],"icon_path":"/Game/Test/Four","IconPaths":"/Game/Test/Three","IconPath":"/Game/Test/Two","character_icon_path":"/Game/Test/One","CharacterIconPath":"/Game/Test/Canonical"}""");
Expect("canonical CharacterIconPath wins over aliases regardless of input order", iconPrecedence.CharacterIconPath == "/Game/Test/Canonical");

var canonicalWeaponIcon = PresetSerializer.Deserialize("""{"Version":3,"UniqueID":"weapon-icon","DisplayName":"Weapon icon","Type":"Weapon","TargetCharacterID":"Astria","Requirements":["None"],"WeaponPath":"/Game/Test/Weapon.Weapon","CharacterIconPath":"/Game/Test/Legacy.Legacy","WeaponIconPath":"/Game/Test/WeaponIcon.WeaponIcon"}""");
Expect("WeaponIconPath wins over legacy CharacterIconPath for Weapon presets",
    canonicalWeaponIcon.WeaponIconPath == "/Game/Test/WeaponIcon.WeaponIcon" && canonicalWeaponIcon.CharacterIconPath is null);
using (var weaponIconJson = JsonDocument.Parse(PresetSerializer.Serialize(canonicalWeaponIcon)))
    Expect("Weapon presets serialize only WeaponIconPath",
        weaponIconJson.RootElement.GetProperty("WeaponIconPath").GetString() == "/Game/Test/WeaponIcon.WeaponIcon" &&
        !weaponIconJson.RootElement.TryGetProperty("CharacterIconPath", out _));

var legacyWeaponIcon = PresetSerializer.Deserialize("""{"Version":3,"UniqueID":"legacy-weapon-icon","DisplayName":"Legacy weapon icon","Type":"Weapon","TargetCharacterID":"Astria","Requirements":["None"],"WeaponPath":"/Game/Test/Weapon.Weapon","IconPath":"/Game/Test/LegacyWeapon.LegacyWeapon"}""");
Expect("legacy Weapon IconPath imports into WeaponIconPath",
    legacyWeaponIcon.WeaponIconPath == "/Game/Test/LegacyWeapon.LegacyWeapon");

var weaponActionSource = PresetSerializer.Deserialize("""{"Version":3,"UniqueID":"weapon-action-source","DisplayName":"Weapon action source","Type":"Weapon","TargetCharacterID":"Onette","Requirements":["None"],"WeaponPath":"/Game/Test/Weapon.Weapon","WeaponIconPath":"/Game/Test/Icon.Icon","weapon_action_source_blueprint_path":"/Game/Test/Legacy.Legacy_C","WeaponActionSourceBlueprintPath":"/Game/Test/Canonical.Canonical_C"}""");
Expect("canonical WeaponActionSourceBlueprintPath wins over its runtime alias",
    weaponActionSource.WeaponActionSourceBlueprintPath == "/Game/Test/Canonical.Canonical_C");
using (var weaponActionJson = JsonDocument.Parse(PresetSerializer.Serialize(weaponActionSource)))
    Expect("Weapon action source serializes canonically without its alias",
        weaponActionJson.RootElement.GetProperty("WeaponActionSourceBlueprintPath").GetString() == "/Game/Test/Canonical.Canonical_C" &&
        !weaponActionJson.RootElement.TryGetProperty("weapon_action_source_blueprint_path", out _));
var nonWeaponActionSource = ValidCostume();
nonWeaponActionSource.WeaponActionSourceBlueprintPath = "/Game/Test/ActionSource.ActionSource_C";
Expect("Weapon action source on a non-Weapon preset is reported",
    validator.Validate(nonWeaponActionSource, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS019"));

var invalidCostumeIcon = PresetSerializer.Deserialize("""{"Version":3,"UniqueID":"wrong-icon-field","DisplayName":"Wrong icon field","Type":"Costume","TargetCharacterID":"Astria","Requirements":["None"],"BodyPath":"/Game/Test/Body.Body","FaceMorphPath":"/Game/Test/Face.Face","WeaponIconPath":"/Game/Test/WeaponIcon.WeaponIcon"}""");
Expect("non-Weapon WeaponIconPath is not accepted as CharacterIconPath",
    invalidCostumeIcon.CharacterIconPath is null &&
    validator.Validate(invalidCostumeIcon, "DSMS-Test.json").Issues.Any(x => x.Field == "CharacterIconPath"));

var displayFallback = PresetPresentation.Resolve(new() { UniqueID = "fallback_id", DisplayName = "Astria  [Costume] - Blue [DLC]" });
var uniqueIdFallback = PresetPresentation.Resolve(new() { UniqueID = "fallback_id", DisplayName = " [Hidden] " });
var explicitPresentation = PresetPresentation.Resolve(new() { UniqueID = "ignored", DisplayName = "Ignored", PresetName = " Explicit name ", PresetVariant = "   " });
Expect("PresetName fallback removes bracketed segments and collapses whitespace", displayFallback.Name == "Astria - Blue");
Expect("PresetName fallback uses UniqueID when DisplayName becomes empty", uniqueIdFallback.Name == "fallback_id");
Expect("explicit PresetName is trimmed and blank PresetVariant is hidden", explicitPresentation.Name == "Explicit name" && explicitPresentation.Variant is null);

var generalVariant = ValidCostume(); generalVariant.PresetVariant = new string('A', 34);
var exceptionalVariant = ValidCostume(); exceptionalVariant.PresetVariant = new string('B', 36);
var overflowingVariant = ValidCostume(); overflowingVariant.PresetVariant = new string('C', 37);
Expect("34-character PresetVariant stays inside the general preview limit",
    !validator.Validate(generalVariant, "DSMS-Test.json").Issues.Any(x => x.Code is "DSMS047" or "DSMS048"));
Expect("35–36 character PresetVariant is accepted as an informational visual exception",
    validator.Validate(exceptionalVariant, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS047" && x.Severity == ValidationSeverity.Info));
Expect("PresetVariant above 36 characters receives a preview-overflow warning",
    validator.Validate(overflowingVariant, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS048" && x.Severity == ValidationSeverity.Warning));

var invalidBounds = ValidCostume(); invalidBounds.BodyMorphTargets = [new() { MorphName = "invalid_bounds", Value = 1f, Min = 2f, Max = 2f }];
Expect("Min greater than or equal to Max is rejected", validator.Validate(invalidBounds, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS046" && x.Field == "BodyMorphTargets[0].Min"));
var belowRange = ValidCostume(); belowRange.BodyMorphTargets = [new() { MorphName = "below_range", Value = -2f, Min = -1.5f, Max = 3f }];
Expect("Value below the declared range is rejected without clamping", validator.Validate(belowRange, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS035" && x.Field == "BodyMorphTargets[0].Value") && belowRange.BodyMorphTargets[0].Value == -2f);
var aboveRange = ValidCostume(); aboveRange.FaceMorphTargets = [new() { MorphName = "above_range", Value = 4f, Min = -1.5f, Max = 3f }];
Expect("Value above the declared range is rejected", validator.Validate(aboveRange, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS035" && x.Field == "FaceMorphTargets[0].Value"));
var emptyDescription = ValidCostume(); emptyDescription.BodyMorphTargets = [new() { MorphName = "empty_description", Value = 0f, Description = "   " }];
Expect("empty morph Description is rejected", validator.Validate(emptyDescription, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS044" && x.Field == "BodyMorphTargets[0].Description"));
var nonFinite = ValidCostume(); nonFinite.BodyMorphTargets = [new() { MorphName = "non_finite", Value = float.NaN }];
Expect("non-finite morph values are rejected", validator.Validate(nonFinite, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS045" && x.Field == "BodyMorphTargets[0].Value"));
var duplicateMorphs = ValidCostume(); duplicateMorphs.BodyMorphTargets = [new() { MorphName = "Shape", Value = 0f }, new() { MorphName = "shape", Value = 1f }];
Expect("duplicate morph names are rejected case-insensitively", validator.Validate(duplicateMorphs, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS034" && x.Field == "BodyMorphTargets[1].MorphName"));
Expect("non-Boolean Locked aliases are rejected during import", ThrowsJson(aliases08Json.Replace("\"locked\": true", "\"locked\": \"yes\"", StringComparison.Ordinal)));

var morphRepair = ValidCostume();
morphRepair.BodyMorphTargets =
[
    new() { MorphName = " first ", Value = -1f, Description = " first description ", Min = -2f, Max = 3f, Locked = true,
        ExtraFields = new() { ["Future"] = JsonValue("{\"kept\":true}") } },
    new() { MorphName = "FIRST", Value = 2f, Description = "second", Min = -2f, Max = 3f, Locked = false }
];
new PresetRepairer(new CharacterCatalog([])).Repair(morphRepair);
Expect("morph repair trims text without dropping order or metadata",
    morphRepair.BodyMorphTargets.Count == 2 && morphRepair.BodyMorphTargets[0].MorphName == "first" &&
    morphRepair.BodyMorphTargets[0].Description == "first description" && morphRepair.BodyMorphTargets[0].Value == -1f &&
    morphRepair.BodyMorphTargets[0].Min == -2f && morphRepair.BodyMorphTargets[0].Max == 3f &&
    morphRepair.BodyMorphTargets[0].Locked == true && morphRepair.BodyMorphTargets[0].ExtraFields?.ContainsKey("Future") == true &&
    morphRepair.BodyMorphTargets[1].MorphName == "FIRST");

var referenceDirectory = Fixture("KnownPresets");
var referenceFiles = Directory.GetFiles(referenceDirectory, "*.json", SearchOption.AllDirectories);
Expect("all 83 DSMS 0.8.1 known presets are included", referenceFiles.Length == 83);
foreach (var referenceFile in referenceFiles)
{
    var referenceSource = File.ReadAllText(referenceFile);
    var referencePreset = PresetSerializer.Deserialize(referenceSource);
    var referenceSerialized = PresetSerializer.Serialize(referencePreset);
    var referenceReport = validator.Validate(referencePreset, Path.GetFileName(referenceFile));
    Expect($"reference preset {Path.GetFileName(referenceFile)} validates", referenceReport.ErrorCount == 0);
    Expect($"reference preset {Path.GetFileName(referenceFile)} survives a semantic no-loss round trip",
        JsonSemanticallyEqual(referenceSource, referenceSerialized));
    if (referencePreset.UsesWeaponIcon)
        Expect($"weapon reference {Path.GetFileName(referenceFile)} uses WeaponIconPath",
            !string.IsNullOrWhiteSpace(referencePreset.WeaponIconPath) && referencePreset.CharacterIconPath is null);
    else
        Expect($"non-weapon reference {Path.GetFileName(referenceFile)} uses CharacterIconPath",
            !string.IsNullOrWhiteSpace(referencePreset.CharacterIconPath) && referencePreset.WeaponIconPath is null);
}
var weaponActionReferences = referenceFiles
    .Select(referenceFile => PresetSerializer.Deserialize(File.ReadAllText(referenceFile)))
    .Where(reference => !string.IsNullOrWhiteSpace(reference.WeaponActionSourceBlueprintPath))
    .ToArray();
Expect("only the four evidence-backed Ornette and Cerese references declare a weapon action source",
    weaponActionReferences.Length == 4 &&
    weaponActionReferences.All(reference => reference.UsesWeaponIcon &&
        reference.WeaponActionSourceBlueprintPath!.EndsWith("_C", StringComparison.Ordinal)));
Expect("dedicated DSMS 0.8 fixtures cover extended unlocked and fixed locked morphs",
    canonicalBodyMorph.Min == -1.5f && canonicalBodyMorph.Max == 3f && canonicalBodyMorph.Locked == false &&
    aliasesRoundTrip.BodyMorphTargets![0].Min == -2f && aliasesRoundTrip.BodyMorphTargets[0].Max == 4f &&
    aliasesRoundTrip.BodyMorphTargets[0].Locked == true &&
    !string.IsNullOrWhiteSpace(aliasesRoundTrip.BodyMorphTargets[0].Description));

using (var compatibilityDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "compatibility.json"))))
{
    var compatibilityRoot = compatibilityDocument.RootElement;
    Expect("Studio compatibility metadata tests Mod Loader 0.8.1",
        compatibilityRoot.GetProperty("TestedModLoaderVersions").EnumerateArray().Any(x => x.GetString() == "0.8.1"));
    Expect("Studio compatibility metadata recommends Mod Loader 0.8.1",
        compatibilityRoot.GetProperty("RecommendedModLoaderVersion").GetString() == "0.8.1");
}

var repairTarget = ValidCostume(); repairTarget.Version = 2; repairTarget.Type = "Character"; repairTarget.TargetCharacterID = "Kalsion";
repairTarget.PresetName = " Test name "; repairTarget.PresetVariant = " Variant "; repairTarget.CharacterIconPath = "/Game/Test/Icon"; repairTarget.PhysicsAnimBlueprintPath = "/Game/Test/DsABP_Test_Physics"; repairTarget.Requirements = null;
new PresetRepairer(new CharacterCatalog([new() { InternalId = "Cassius", DisplayName = "Kalsion", Aliases = ["Kalsion"], Playable = true }])).Repair(repairTarget);
Expect("safe repair normalizes schema, metadata, alias and object paths", repairTarget.Version == 3 && repairTarget.Type == "Custom" && repairTarget.TargetCharacterID == "Cassius" && repairTarget.PresetName == "Test name" && repairTarget.PresetVariant == "Variant" && repairTarget.CharacterIconPath == "/Game/Test/Icon.Icon" && repairTarget.PhysicsAnimBlueprintPath == "/Game/Test/DsABP_Test_Physics.DsABP_Test_Physics_C");

var weaponActionRepair = weaponActionSource;
weaponActionRepair.WeaponActionSourceBlueprintPath = "BlueprintGeneratedClass'/Game/Test/ActionSource.ActionSource'";
new PresetRepairer(new CharacterCatalog([])).Repair(weaponActionRepair);
Expect("safe repair canonicalizes WeaponActionSourceBlueprintPath",
    weaponActionRepair.WeaponActionSourceBlueprintPath == "/Game/Test/ActionSource.ActionSource_C");

var customSameId = ValidCostume(); customSameId.UniqueID = "Astria_costume_swimsuit01_dlc"; customSameId.BodyPath = "/Game/MODS/OtherAuthor/NewBody.NewBody";
new PresetRepairer(new CharacterCatalog([])).Repair(customSameId);
Expect("custom preset is never replaced by a vanilla reference", customSameId.BodyPath == "/Game/MODS/OtherAuthor/NewBody.NewBody");

var tempIndex = Path.Combine(Path.GetTempPath(), "DSMS-FModelIndex-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(tempIndex, "Art", "Character", "Test")); File.WriteAllText(Path.Combine(tempIndex, "Art", "Character", "Test", "KnownMesh.json"), "{}");
Directory.CreateDirectory(Path.Combine(tempIndex, "Design", "DsCharacter", "Player", "Test")); File.WriteAllText(Path.Combine(tempIndex, "Design", "DsCharacter", "Player", "Test", "ActionSource.json"), "{}");
var index = FModelAssetIndex.Build(tempIndex); var indexedPreset = ValidCostume(); indexedPreset.BodyPath = "/Game/Art/Character/Test/KnownMesh.KnownMesh";
Expect("FModel exact asset is verified", !new FModelPresetValidator(index).Validate(indexedPreset).Issues.Any(x => x.Field == "BodyPath"));
indexedPreset.BodyPath = "/Game/Art/Character/Test/Knownmesh.Knownmesh";
Expect("FModel detects casing differences", new FModelPresetValidator(index).Validate(indexedPreset).Issues.Any(x => x.Code == "DSMS110"));
indexedPreset.BodyPath = "/Game/MODS/Author/Unknown.Unknown";
Expect("missing custom asset is informational, not invalid", new FModelPresetValidator(index).Validate(indexedPreset).Issues.Any(x => x.Code == "DSMS112"));
var indexedWeapon = weaponActionSource;
indexedWeapon.WeaponActionSourceBlueprintPath = "/Game/Design/DsCharacter/Player/Test/ActionSource.ActionSource_C";
Expect("FModel verifies a generated Weapon action-source class through its package export",
    !new FModelPresetValidator(index).Validate(indexedWeapon).Issues.Any(x => x.Field == "WeaponActionSourceBlueprintPath"));
Directory.Delete(tempIndex, true);

if (args.Length > 0)
{
    var repository = new PresetRepositoryValidator(validator).Scan(args[0]);
    Console.WriteLine($"Repository: {repository.FileCount} presets, {repository.ErrorCount} errors, {repository.WarningCount} warnings");
    foreach (var file in repository.Files.Where(x => x.Report.ErrorCount > 0 || x.Report.WarningCount > 0))
        foreach (var issue in file.Report.Issues)
            Console.WriteLine($"[{issue.Severity}] {Path.GetFileName(file.FilePath)} {issue.Code} {issue.Field}: {issue.Message}");
    Expect("repository contains presets", repository.FileCount > 0);
    Expect("repository scan reports issues without modifying source files", repository.Files.All(x => File.Exists(x.FilePath)));
    foreach (var file in repository.Files.Where(x => x.Preset is not null))
    {
        var source = File.ReadAllText(file.FilePath);
        var serialized = PresetSerializer.Serialize(file.Preset!);
        Expect($"repository preset {Path.GetFileName(file.FilePath)} survives a semantic no-loss round trip",
            JsonSemanticallyEqual(source, serialized));
    }
}
if (args.Length > 1)
{
    var realIndex = FModelAssetIndex.Build(args[1]);
    Console.WriteLine($"FModel index: {realIndex.Count} exported assets");
    Expect("real FModel index contains Astria swimsuit body",
        realIndex.Lookup("/Game/Art/Character/Costume/DS_Astria/Astira_swimsuit_01/Mesh/ch_Astria_swimsuit_01_body_mesh.ch_Astria_swimsuit_01_body_mesh").Kind == AssetLookupKind.Exact);
}
if (failures.Count == 0) { Console.WriteLine("All DSMS Core self-tests passed."); return 0; }
Console.Error.WriteLine($"Failed: {string.Join(", ", failures)}"); return 1;
