# DSMS Preset Studio Changelog

A Windows desktop preset builder and validator for **DragonSword: Awakening**, **DSMS JSON v3** and compatible DSMS ModLoader releases.

## Version 0.5.2

- Removes the unsafe known-preset replacement system introduced in 0.5.1. A custom preset is never replaced from its `UniqueID` or filename.
- Adds strict Unreal object-path validation: package asset and object names must match exactly, including spelling and case.
- Adds the Animation Blueprint generated-class rule (`Asset.Asset_C`).
- Adds optional, manual FModel export indexing for exact asset existence, spelling and case verification.
- Treats missing `/Game/MODS/` assets as unverified information rather than an error, so original custom assets remain safe.
- Applies only deterministic repairs such as separators, whitespace, known character aliases, missing object suffixes and `_C` suffixes.
- Keeps every repair unsaved for author review before saving.

## Version 0.5.0

- Completes the French interface translation while keeping technical JSON field names unchanged.
- Adds reference-friendly material tables, expandable material paths and improved icon previews.
- Adds safe path normalization and the first assisted-repair pass.

## Version 0.4.0

- Adds an icon preview system with FModel-export resolution, manual imports and type-specific fallback icons.
- Adds an external compatibility profile and local DSMS ModLoader version detection; Studio and ModLoader versions remain independent.
- Adds a GitHub release checker and confirmed self-update flow with mandatory SHA-256 verification and rollback support.
- Adds FModel export and ModLoader path settings.
- Adds the Castella application icon to the executable, title bar and Windows taskbar.
- Preserves `Icons/Imported` and Local AppData settings during application updates.

## Version 0.3.1

- Fixes Recipe, Character and Theme selectors so they show human-readable names instead of .NET class names.
- Synchronizes the active recipe when an existing JSON preset is opened.
- Makes the four Visual Builder navigation steps interactive and visually persistent.
- Restores the intended toolbar and workspace tab proportions.
- Adds a dependency-free, theme-aware JSON syntax editor.
- Completes custom themes with editable primary/secondary text colors, hexadecimal color fields, font family and base font size.
- Adds the HoverMods Vault Patreon link to About.
- Adds complete Windows application metadata with `HoverMods Vault` as the company.

## Version 0.3.0

- Replaces the native light WPF controls with a complete high-contrast dark control system.
- Reorganizes the Visual Builder into the modern four-step layout used by the approved design mockup.
- Adds persistent appearance and language settings.
- Includes three built-in themes and supports custom JSON themes with an optional embedded background image.
- Includes an offline HTML Theme Designer under `Tools/ThemeDesigner`.
- Supports English (UK) and French descriptions while preserving all technical DSMS field names in English.
- Adds the HoverMods Vault About panel, explicit application/ModLoader versions and an offline placeholder for future GitHub update checks.
- Adds validation result filters for errors, warnings and information.

## Builder and validator

- Builds common Costume, Custom and Weapon presets through a guided visual form.
- Provides an external character catalog with the important public/internal aliases (`Kalsion`/`Cassius`, `Ornette`/`Onette`).
- Provides recipes for full costumes, retextures, single/multi-mesh weapons, dedicated outlines and auxiliary meshes.
- Edits Body, Face, Outline and Weapon material slots in separate tables.
- Generates a clean JSON v3 document while preserving advanced fields loaded from an existing preset.
- Opens and formats DSMS JSON v3 presets.
- Validates required fields, Unreal object paths, target IDs, material slots, morph targets, weapons, auxiliary meshes and body outlines.
- Scans an entire preset folder and detects duplicate `UniqueID` values.
- Uses green, orange and red risk levels.
- Ships character and recipe data separately so the catalog can evolve without redesigning the application.

For bundled known presets, the reference database can detect and restore subtle differences. For a new custom asset, a green result still cannot prove that two skeletal assets share a compatible skeleton, physics setup or Animation Blueprint. Always test mods on a backup save.

The application never writes to the game directory unless the user explicitly chooses that location in **Save as**.
