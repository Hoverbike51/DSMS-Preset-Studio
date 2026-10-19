# DSMS Preset Studio

**DSMS Preset Studio** is a Windows desktop application for creating, reviewing and validating **DragonSword: Awakening** presets for **DSMS ModLoader**.

- Current Studio version: **0.6.1**
- Preset format: **DSMS JSON v3**
- Recommended ModLoader version: **0.8.1**

Version 0.6.1 promotes the unchanged JSON v3 authoring contract to the locally validated DSMS Mod Loader 0.8.1 line. It adds no-loss round-trip coverage for the current preset corpus and presentation-length guidance without imposing a runtime-incompatible schema maximum.

## What the application does

- Creates Costume, Custom and Weapon presets through a guided visual builder.
- Opens and edits existing DSMS JSON v3 files.
- Generates consistently formatted JSON.
- Validates required fields, target character IDs, Unreal paths, material slots, morph targets, body and face outlines, auxiliary meshes and weapon components.
- Edits Body and Face Morph Targets with optional descriptions, custom minimum/maximum ranges and runtime locking.
- Authors the optional `PresetName` and `PresetVariant` details metadata while keeping `DisplayName` as the selector label, with a live 34-character general-limit indicator and an explicit 35–36-character exception range.
- Reads legacy icon aliases and writes `CharacterIconPath` for non-Weapon presets or `WeaponIconPath` for Weapon presets.
- Authors the optional Weapon-only `WeaponActionSourceBlueprintPath` used by Mod Loader 0.8+ to copy an evidence-backed `WeaponActionDataArray` from a source Blueprint.
- Preserves unknown preset and nested object properties during load, edit and save.
- Applies only deterministic and reviewable repairs.
- Scans preset folders and reports duplicate `UniqueID` values.
- Optionally verifies exported game assets against an FModel export folder.
- Displays game icons exported by FModel or manually imported images.
- Detects the installed DSMS ModLoader version.
- Supports English and French interfaces, built-in themes and custom themes.

Preset Studio never replaces a custom preset with a vanilla database entry. Every repair remains unsaved until the author reviews and saves the result.

## Installation

1. Download the latest portable ZIP from the GitHub Releases page.
2. Verify its published SHA-256 checksum.
3. Extract the entire archive into a writable folder.
4. Run `DSMS.PresetStudio.exe`.

Keep the `Data`, `Icons`, `Tools` and `compatibility.json` items beside the executable.

## Recommended workflow

### 1. Start or open a preset

- Use **New** to start from a clean recipe.
- Use **Open JSON** to edit one preset.
- Use **Scan folder** to inspect a complete preset collection.

### 2. Select a recipe

The Visual Builder provides recipes for:

- full costume replacement;
- costume retexture;
- single-mesh and multi-component weapons;
- weapon retexture;
- custom body with dedicated body and optional face outlines;
- auxiliary meshes and native-component hiding.

Select the target character before entering asset paths. Public character names and internal aliases such as `Kalsion/Cassius` and `Ornette/Onette` are handled by the character catalog.

`PresetName` and `PresetVariant` control the Mod Loader details panel. When `PresetName` is blank, Studio previews the runtime fallback obtained by removing bracketed segments from `DisplayName`; if that becomes empty, it uses `UniqueID`. A blank `PresetVariant` stays hidden. Keep `PresetVariant` at 34 characters or fewer in general; 35–36 characters are accepted only as a visually confirmed exception. Longer values remain valid JSON but receive an overflow warning. `DisplayName` itself remains unchanged and continues to identify the preset in selectors.

### 3. Enter Unreal asset paths

Use Unreal object paths, never Windows paths:

```text
/Game/MODS/ModAuthor/ModName/Mesh/Character_body.Character_body
```

The package asset and object name must normally match exactly. Animation Blueprint generated classes use:

```text
/Game/Path/DsABP_Physics.DsABP_Physics_C
```

For Weapon presets, `WeaponActionSourceBlueprintPath` uses the same generated-class form. Leave it empty unless FModel and runtime testing prove that the selected source Blueprint owns the compatible `WeaponActionDataArray`; Studio never guesses this path from a mesh name.

Use **Material Instances only**, and make sure every `SlotIndex` matches the selected Skeletal Mesh.

### 4. Generate and validate

Use **Generate JSON**, then **Validate & repair**.

Validation results use three levels:

- **Error**: the preset is structurally invalid and should not be used.
- **Warning**: the preset may work, but the reported value requires review.
- **Information**: the value could not be proven or a safe normalization was applied.

Safe repairs include whitespace cleanup, slash normalization, known character aliases, missing object suffixes and missing Animation Blueprint `_C` suffixes. Studio does not invent unknown asset paths or silently restore a vanilla preset.

### 5. Save and install the preset

Every preset filename must begin with `DSMS-`.

Install the saved JSON anywhere below:

```text
DragonSword Awakening\DS\Content\Paks\~mods\HMV_DS_SELECTOR\
```

Subfolders are scanned recursively by DSMS ModLoader.

## FModel verification

FModel indexing is optional and manual.

1. Export the required DragonSword assets with FModel.
2. In **Settings → Asset sources**, select the exported `DS/Content` folder.
3. Click **Index FModel exports**.
4. Validate the preset again.

Studio can then verify asset existence, spelling and case against the files actually exported by FModel.

Missing vanilla assets produce a warning. Missing assets under `/Game/MODS/` remain **unverified information**, because custom mods are not necessarily present in the selected FModel export.

FModel verification improves confidence but cannot prove runtime skeleton compatibility, physics behaviour or Animation Blueprint correctness.

## Icon preview

Studio resolves the type-specific icon path from the configured FModel export folder: `CharacterIconPath` for non-Weapon presets and `WeaponIconPath` for Weapon presets. Legacy `character_icon_path`, `weapon_icon_path`, `IconPath`, `IconPaths` and `icon_path` fields remain importable; an older Weapon `CharacterIconPath` is migrated automatically. Generated JSON writes only the correct canonical field. Short `/Game/.../Asset` paths are normalized to `/Game/.../Asset.Asset`. You can also import a PNG manually; it is copied into `Icons/Imported`.

When no compatible image is found, Studio displays the generic Costume, Custom or Weapon icon.

## Settings

- Choose English or French.
- Browse all bundled and imported themes with their name, author and provenance. Bundled themes are read-only `Official Theme System` themes authored by `HoverModsVault`.
- Keep multiple custom themes, edit them in the offline HTML Theme Designer, and preview Visual Builder, JSON Editor and Settings.
- Adjust UI and text opacity independently. UI opacity controls surfaces, borders, controls and status backgrounds; text opacity controls primary, secondary, accent, semantic and JSON token text.
- Use a system theme as a base only through a renamed custom copy; exported/imported themes never receive official certification.
- Configure the FModel export root.
- Configure or detect the DSMS ModLoader Scripts folder.
- Open the imported-icons folder.
- Enable or disable automatic GitHub update checks at startup, or check manually.

Updates require confirmation and a valid SHA-256 checksum. Imported icons and settings stored under `%LocalAppData%\HoverModsVault\DSMSPresetStudio` are preserved.

## Safety and limitations

- Always test presets in game before publication.
- Back up game saves before mod development.
- A green result means the JSON is structurally valid; it does not guarantee that every referenced asset is compatible at runtime.
- Studio does not modify the game directory unless you explicitly select it with **Save as**.
- Internet access is only required when Studio checks GitHub for updates, automatically or manually.

## Projects and documentation

- DSMS ModLoader: https://github.com/Hoverbike51/DSMS-ModLoader
- Studio releases: https://github.com/Hoverbike51/DSMS-Preset-Studio/releases
- Version history: [CHANGELOG.md](CHANGELOG.md)

## Development

```powershell
dotnet build .\DSMS-Preset-Studio.sln -c Release
dotnet run --project .\DSMS.Core.Tests -- "PATH_TO_PRESET_FOLDER"
dotnet run --project .\DSMS.PresetStudio
```
