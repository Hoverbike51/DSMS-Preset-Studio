namespace DSMS.PresetStudio.Localization;

public static class DetailLocalizer
{
    public static string Get(string key, string language) => (language, key) switch
    {
        ("fr-FR", "SettingsIntro") => "Personnalisez l’apparence et la langue. Vous pouvez aussi indexer manuellement les exports FModel afin de vérifier l’existence, la casse et l’orthographe des chemins Unreal sans écraser les presets personnalisés.",
        ("fr-FR", "ThemeHelp") => "Choisissez un thème intégré ou importez un thème JSON créé avec le concepteur HTML. Les couleurs du texte, la police, sa taille et une image de fond facultative peuvent être personnalisées.",
        ("fr-FR", "LanguageHelp") => "La langue traduit l’intégralité de l’interface, des descriptions et des diagnostics. Les clés JSON, types DSMS et chemins Unreal restent techniquement inchangés.",
        ("fr-FR", "About") => "DSMS Preset Studio est un outil HoverMods Vault conçu pour créer et vérifier des presets JSON v3 destinés à DSMS ModLoader.",
        ("fr-FR", "Updates") => "Vérifiez manuellement les publications GitHub. Une mise à jour n’est installée qu’après votre confirmation et la validation de son empreinte SHA-256.",
        ("fr-FR", "MaterialHint") => "Material Instances uniquement. Les index doivent correspondre exactement aux slots du Skeletal Mesh sélectionné.",
        ("fr-FR", "NoIssues") => "Aucun problème détecté. La configuration du preset est structurellement valide.",
        (_, "SettingsIntro") => "Customize appearance and language. You can also manually index FModel exports to verify Unreal path existence, casing and spelling without overwriting custom presets.",
        (_, "ThemeHelp") => "Choose a built-in theme or import a JSON theme made with the HTML designer. Text colors, font family, font size and an optional background image can be customized.",
        (_, "LanguageHelp") => "Language changes the complete interface, descriptions and diagnostics. JSON keys, DSMS values and Unreal paths remain technically unchanged.",
        (_, "About") => "DSMS Preset Studio is a HoverMods Vault tool for creating and validating JSON v3 presets for DSMS ModLoader.",
        (_, "Updates") => "Check GitHub releases manually. An update is installed only after your confirmation and successful SHA-256 verification.",
        (_, "MaterialHint") => "Material Instances only. Slot indexes must exactly match the selected Skeletal Mesh.",
        (_, "NoIssues") => "No issues detected. The preset configuration is structurally valid.",
        _ => key
    };
}
