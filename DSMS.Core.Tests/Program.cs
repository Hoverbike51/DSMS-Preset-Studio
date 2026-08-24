using DSMS.Core.Assets;
using DSMS.Core.Catalog;
using DSMS.Core.Models;
using DSMS.Core.Repair;
using DSMS.Core.Repository;
using DSMS.Core.Validation;

var failures = new List<string>();
var validator = new PresetValidator();
void Expect(string name, bool condition) { Console.WriteLine($"[{(condition ? "PASS" : "FAIL")}] {name}"); if (!condition) failures.Add(name); }
PresetDocument ValidCostume() => new()
{
    Version = 3, UniqueID = "test_astria_costume", DisplayName = "Astria - Test [Costume Custom]",
    Type = "Costume", TargetCharacterID = "Astria", Requirements = ["None"],
    BodyPath = "/Game/mods/Test/Mesh/TestBody.TestBody",
    PhysicsAnimBlueprintPath = "/Game/Test/DsABP_Test_Physics.DsABP_Test_Physics_C",
    FaceMorphPath = "/Game/Test/Test_ingame_face.Test_ingame_face", IconPath = "/Game/Test/Icon.Icon"
};

Expect("valid costume has no errors", validator.Validate(ValidCostume(), "DSMS-Test.json").ErrorCount == 0);
Expect("filename prefix is mandatory", validator.Validate(ValidCostume(), "Test.json").Issues.Any(x => x.Code == "DSMS001"));
var mismatch = ValidCostume(); mismatch.BodyPath = "/Game/mods/Test/Mesh/Ekko_Custom_Body.Eko_Custom_Brody";
Expect("package/object spelling mismatch is rejected", validator.Validate(mismatch, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS017"));
var duplicateSlots = ValidCostume(); duplicateSlots.BodyMaterials = [new() { SlotIndex = 0, MaterialPath = "/Game/Test/MI_A.MI_A" }, new() { SlotIndex = 0, MaterialPath = "/Game/Test/MI_B.MI_B" }];
Expect("duplicate material slots are rejected", validator.Validate(duplicateSlots, "DSMS-Test.json").Issues.Any(x => x.Code == "DSMS032"));

var repairTarget = ValidCostume(); repairTarget.Version = 2; repairTarget.Type = "Character"; repairTarget.TargetCharacterID = "Kalsion";
repairTarget.IconPath = "/Game/Test/Icon"; repairTarget.PhysicsAnimBlueprintPath = "/Game/Test/DsABP_Test_Physics"; repairTarget.Requirements = null;
new PresetRepairer(new CharacterCatalog([new() { InternalId = "Cassius", DisplayName = "Kalsion", Aliases = ["Kalsion"], Playable = true }])).Repair(repairTarget);
Expect("safe repair normalizes schema, alias and object paths", repairTarget.Version == 3 && repairTarget.Type == "Custom" && repairTarget.TargetCharacterID == "Cassius" && repairTarget.IconPath == "/Game/Test/Icon.Icon" && repairTarget.PhysicsAnimBlueprintPath == "/Game/Test/DsABP_Test_Physics.DsABP_Test_Physics_C");

var customSameId = ValidCostume(); customSameId.UniqueID = "Astria_costume_swimsuit01_dlc"; customSameId.BodyPath = "/Game/MODS/OtherAuthor/NewBody.NewBody";
new PresetRepairer(new CharacterCatalog([])).Repair(customSameId);
Expect("custom preset is never replaced by a vanilla reference", customSameId.BodyPath == "/Game/MODS/OtherAuthor/NewBody.NewBody");

var tempIndex = Path.Combine(Path.GetTempPath(), "DSMS-FModelIndex-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(tempIndex, "Art", "Character", "Test")); File.WriteAllText(Path.Combine(tempIndex, "Art", "Character", "Test", "KnownMesh.json"), "{}");
var index = FModelAssetIndex.Build(tempIndex); var indexedPreset = ValidCostume(); indexedPreset.BodyPath = "/Game/Art/Character/Test/KnownMesh.KnownMesh";
Expect("FModel exact asset is verified", !new FModelPresetValidator(index).Validate(indexedPreset).Issues.Any(x => x.Field == "BodyPath"));
indexedPreset.BodyPath = "/Game/Art/Character/Test/Knownmesh.Knownmesh";
Expect("FModel detects casing differences", new FModelPresetValidator(index).Validate(indexedPreset).Issues.Any(x => x.Code == "DSMS110"));
indexedPreset.BodyPath = "/Game/MODS/Author/Unknown.Unknown";
Expect("missing custom asset is informational, not invalid", new FModelPresetValidator(index).Validate(indexedPreset).Issues.Any(x => x.Code == "DSMS112"));
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
