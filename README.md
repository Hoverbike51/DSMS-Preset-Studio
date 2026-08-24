# DSMS Preset Studio

**DSMS Preset Studio** is a Windows desktop application for creating, reviewing and validating **DragonSword: Awakening** presets for **DSMS ModLoader**.

Current Studio version: **0.5.2**  
Preset format: **DSMS JSON v3**  
Recommended ModLoader version: **0.7.1**

## What the application does

- Creates Costume, Custom and Weapon presets through a guided visual builder.
- Opens and edits existing DSMS JSON v3 files.
- Generates consistently formatted JSON.
- Validates required fields, target character IDs, Unreal paths, material slots, morph targets, outlines, auxiliary meshes and weapon components.
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
- custom body with a dedicated outline;
- auxiliary meshes and native-component hiding.

Select the target character before entering asset paths. Public character names and internal aliases such as `Kalsion/Cassius` and `Ornette/Onette` are handled by the character catalog.

### 3. Enter Unreal asset paths

Use Unreal object paths, never Windows paths:

```text
/Game/MODS/ModAuthor/ModName/Mesh/Character_body.Character_body
```

The package asset and object name must normally match exactly. Animation Blueprint generated classes use:

```text
/Game/Path/DsABP_Physics.DsABP_Physics_C
```

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

Studio resolves `IconPath` from the configured FModel export folder. You can also import a PNG manually; it is copied into `Icons/Imported`.

When no compatible image is found, Studio displays the generic Costume, Custom or Weapon icon.

## Settings

- Choose English or French.
- Select a built-in theme or import a custom theme.
- Open the included offline HTML Theme Designer.
- Configure the FModel export root.
- Configure or detect the DSMS ModLoader Scripts folder.
- Open the imported-icons folder.
- Check GitHub manually for Studio updates.

Updates require confirmation and a valid SHA-256 checksum. Imported icons and settings stored under `%LocalAppData%\HoverModsVault\DSMSPresetStudio` are preserved.

## Safety and limitations

- Always test presets in game before publication.
- Back up game saves before mod development.
- A green result means the JSON is structurally valid; it does not guarantee that every referenced asset is compatible at runtime.
- Studio does not modify the game directory unless you explicitly select it with **Save as**.
- Internet access is only required for the manual GitHub update check.

## Projects and documentation

- DSMS ModLoader: https://github.com/Hoverbike51/DSMS-ModLoader
- Studio releases: https://github.com/Hoverbike51/DSMS-Preset-Studio/releases
- Virus Total: https://www.virustotal.com/gui/file/56b7125fec4e4223362451e033d0730ff178b65810ceb1b67de046119f185f46
- Version history: [CHANGELOG.md](CHANGELOG.md)
