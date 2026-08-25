# DSMS Preset Studio — Changelog

## Version 0.5.3

- Adds full DSMS ModLoader 0.7.2 support for `FaceOutlinePath`, `FaceOutlineMaterials` and `FaceOutlineClearMaterialOverrides` across the model, visual builder, validation, repair, FModel checks and JSON schema.
- Adds 0.7.2 to the tested ModLoader versions and makes it the recommended release.
- Adds an optional automatic GitHub release check at application startup with a theme-aware confirmation window.
- Applies explicit Update state colors: READY/success `#35D07F`, CHECKING/warning `#F5B942`, CURRENT/theme base and failures/error `#FF6174`.
- Rebuilds the Theme Designer preview around the real Visual Builder, JSON Editor and Settings layouts, with functional preview navigation.
- Adds independent UI and text opacity controls to custom themes.
- Adds a persistent multi-theme library with visible theme names and authors.
- Certifies bundled themes as read-only `Official Theme System` themes authored by `HoverModsVault`; imported JSON can never claim this certification.
- Allows bundled themes to be previewed or exported only as renamed, non-certified custom copies.
- Makes Theme Designer load installed system/custom themes and edit custom themes while protecting reserved system names.
- Fixes JSON Editor token colors so they follow the selected theme and consistently respect text opacity.

## Version 0.5.2

- Removes the unsafe known-preset replacement system introduced in 0.5.1. A custom preset is never replaced from its `UniqueID` or filename.
- Adds strict Unreal object-path validation: package asset and object names must match exactly, including spelling and case.
- Adds the Animation Blueprint generated-class rule (`Asset.Asset_C`).
- Adds optional, manual FModel export indexing for exact asset existence, spelling and case verification.
- Treats missing `/Game/MODS/` assets as unverified information rather than an error, so original custom assets remain safe.
- Applies only deterministic repairs such as separators, whitespace, known character aliases, missing object suffixes and `_C` suffixes.
- Keeps every repair unsaved for author review before saving.

## Version 0.5.1 — Withdrawn

- This release attempted to use known presets as an authoritative repair database.
- It could replace valid custom values when a preset reused a known `UniqueID` or filename.
- The release was rejected and completely superseded by version 0.5.2.

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
---
