using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using DSMS.Core.Validation;

namespace DSMS.PresetStudio.Localization;

public static class UiLocalizer
{
    public static string CurrentLanguage { get; private set; } = "en-GB";
    private sealed class OriginalText(string value) { public string Value { get; } = value; }
    private static readonly ConditionalWeakTable<object, OriginalText> Originals = new();

    private static readonly Dictionary<string, string> French = new(StringComparer.Ordinal)
    {
        ["▣  New"] = "▣  Nouveau",
        ["▱  Open JSON"] = "▱  Ouvrir un JSON",
        ["⌕  Scan folder"] = "⌕  Analyser un dossier",
        ["</>  Generate JSON"] = "</>  Générer le JSON",
        ["◇  Validate"] = "◇  Valider et corriger",
        ["◇  Validate & repair"] = "◇  Valider et corriger",
        ["≡  Format"] = "≡  Formater",
        ["▤  Save as"] = "▤  Enregistrer sous",
        ["New unsaved preset"] = "Nouveau preset non enregistré",
        ["Visual Builder"] = "Constructeur visuel",
        ["Identity"] = "Identité",
        ["Preset identity"] = "Identité du preset",
        ["Body & Face"] = "Corps et visage",
        ["Paths & meshes"] = "Chemins et meshes",
        ["Materials"] = "Matériaux",
        ["Material instances"] = "Material Instances",
        ["Advanced"] = "Avancé",
        ["Options & requirements"] = "Options et prérequis",
        ["QUICK SAFETY"] = "SÉCURITÉ RAPIDE",
        ["Use full Unreal object paths. Never paste Windows paths in a preset."] = "Utilisez des chemins d’objet Unreal complets. Ne collez jamais de chemins Windows dans un preset.",
        ["♙  Preset identity"] = "♙  Identité du preset",
        ["Recipe"] = "Recette",
        ["Preset type"] = "Type de preset",
        ["Character"] = "Personnage",
        ["TargetCharacterID"] = "Personnage ciblé (TargetCharacterID)",
        ["UniqueID"] = "Identifiant unique (UniqueID)",
        ["DisplayName"] = "Nom affiché (DisplayName)",
        ["▱  Paths"] = "▱  Chemins",
        ["BodyPath"] = "Chemin du corps (BodyPath)",
        ["FaceMorphPath"] = "Visage en jeu (FaceMorphPath)",
        ["FacePath (optional)"] = "Visage secondaire (FacePath, facultatif)",
        ["PhysicsAssetPath"] = "Physics Asset (PhysicsAssetPath)",
        ["Physics ABP"] = "Animation Blueprint de physique",
        ["IconPath"] = "Chemin de l’icône (IconPath)",
        ["⚙  Advanced meshes"] = "⚙  Meshes avancés",
        ["BodyOutlinePath"] = "Outline du corps (BodyOutlinePath)",
        ["FaceOutlinePath"] = "Outline du visage (FaceOutlinePath)",
        ["AuxiliaryMeshPath"] = "Mesh auxiliaire (AuxiliaryMeshPath)",
        ["WeaponPath"] = "Chemin de l’arme (WeaponPath)",
        ["♙  Active recipe"] = "♙  Recette active",
        ["▣  Icon preview"] = "▣  Aperçu de l’icône",
        ["Import image…"] = "Importer une image…",
        ["Apply suggested path"] = "Appliquer le chemin suggéré",
        ["▰  Material instances"] = "▰  Material Instances",
        ["Body"] = "Corps",
        ["Face"] = "Visage",
        ["Body Outline"] = "Outline du corps",
        ["Face Outline"] = "Outline du visage",
        ["Weapon"] = "Arme",
        ["Slot"] = "Index",
        ["Material instance path"] = "Chemin de la Material Instance",
        ["Current material match (optional)"] = "Matériau actuel ciblé (facultatif)",
        ["▱  Options"] = "▱  Options",
        ["Clear native body material overrides"] = "Effacer les matériaux natifs du corps",
        ["Clear native face material overrides"] = "Effacer les matériaux natifs du visage",
        ["Clear body outline material overrides"] = "Effacer les matériaux de l’outline du corps",
        ["Clear face outline material overrides"] = "Effacer les matériaux de l’outline du visage",
        ["Clear native weapon material overrides"] = "Effacer les matériaux natifs de l’arme",
        ["Weapon retexture only (keep native mesh)"] = "Retexture d’arme uniquement (conserver le mesh natif)",
        ["Requirements (comma-separated)"] = "Prérequis (séparés par des virgules)",
        ["JSON Editor"] = "Éditeur JSON",
        ["ADVANCED MODE"] = "MODE AVANCÉ",
        ["Edit every JSON v3 field directly. Format and validate before returning to the Visual Builder."] = "Modifiez directement tous les champs JSON v3. Formatez puis validez avant de revenir au constructeur visuel.",
        ["Technical field names and Unreal paths are never localized."] = "Les noms techniques des champs et les chemins Unreal restent inchangés dans le document JSON.",
        ["Settings"] = "Paramètres",
        ["⚙  Settings"] = "⚙  Paramètres",
        ["◈  Theme"] = "◈  Thème",
        ["Apply theme"] = "Appliquer le thème",
        ["Import theme…"] = "Importer un thème…",
        ["Open HTML designer"] = "Ouvrir le créateur HTML",
        ["A  Language"] = "A  Langue",
        ["▣  Asset sources"] = "▣  Sources des assets",
        ["FModel export root (select the DS/Content folder)"] = "Racine des exports FModel (sélectionnez le dossier DS/Content)",
        ["Index FModel exports"] = "Indexer les exports FModel",
        ["Browse…"] = "Parcourir…",
        ["DSMS ModLoader Scripts folder"] = "Dossier Scripts de DSMS ModLoader",
        ["Detect ModLoader"] = "Détecter ModLoader",
        ["Open Icons folder"] = "Ouvrir le dossier Icons",
        ["ⓘ  About"] = "ⓘ  À propos",
        ["APPLICATION"] = "APPLICATION",
        ["Open DSMS ModLoader on GitHub"] = "Ouvrir DSMS ModLoader sur GitHub",
        ["Visit HoverMods Vault on Patreon"] = "Visiter HoverMods Vault sur Patreon",
        ["↻  Updates"] = "↻  Mises à jour",
        ["Check for updates when DSMS Preset Studio starts"] = "Rechercher les mises à jour au démarrage de DSMS Preset Studio",
        ["GitHub release service"] = "Service de publication GitHub",
        ["Ready to check"] = "Prêt à vérifier",
        ["READY"] = "PRÊT",
        ["Check for updates"] = "Rechercher des mises à jour",
        ["Download and install"] = "Télécharger et installer",
        ["◇  Validation results"] = "◇  Résultats de validation",
        ["All "] = "Tous ",
        ["Errors "] = "Erreurs ",
        ["Warnings "] = "Avertissements ",
        ["Level"] = "Niveau",
        ["File / Field"] = "Fichier / Champ",
        ["Message"] = "Message",
        ["Expand materials"] = "Agrandir les matériaux",
        ["Restore layout"] = "Restaurer la disposition"
    };

    public static bool IsFrench(string language) => language.Equals("fr-FR", StringComparison.OrdinalIgnoreCase);

    public static string Text(string english, string language) =>
        IsFrench(language) && French.TryGetValue(english, out var translated) ? translated : english;

    public static void Apply(DependencyObject root, string language)
    {
        CurrentLanguage = language;
        ApplyObject(root, language);
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            Apply(child, language);
    }

    private static void ApplyObject(object value, string language)
    {
        switch (value)
        {
            case TextBlock textBlock:
                textBlock.Text = TranslateStored(textBlock, textBlock.Text, language);
                break;
            case Button { Content: string content } button:
                button.Content = TranslateStored(button, content, language);
                break;
            case CheckBox { Content: string content } checkBox:
                checkBox.Content = TranslateStored(checkBox, content, language);
                break;
            case TabItem { Header: string header } tab:
                tab.Header = TranslateStored(tab, header, language);
                break;
        }
    }

    private static string TranslateStored(object owner, string current, string language)
    {
        if (!Originals.TryGetValue(owner, out var original))
        {
            original = new OriginalText(current);
            Originals.Add(owner, original);
        }
        return Text(original.Value, language);
    }

    public static string RecipeName(string recipeId, string fallback, string language)
    {
        if (!IsFrench(language)) return fallback;
        return recipeId switch
        {
            "costume-full" => "Remplacement complet de costume",
            "costume-retexture" => "Retexture de costume",
            "weapon-single" => "Arme à mesh unique",
            "weapon-multi" => "Arme à plusieurs composants",
            "weapon-retexture" => "Retexture des matériaux d’arme",
            "custom-outline" => "Corps personnalisé avec outline dédié",
            "auxiliary-hide" => "Mesh auxiliaire et masquage de composants",
            _ => fallback
        };
    }

    public static ValidationIssue Validation(ValidationIssue issue, string language)
    {
        if (!IsFrench(language)) return issue;
        var message = issue.Code switch
        {
            "DSMS001" => "Le nom du fichier doit commencer par « DSMS- ».",
            "DSMS002" => "Le profil de compatibilité actif exige la version 3 du schéma JSON.",
            "DSMS003" => "UniqueID est obligatoire.",
            "DSMS004" => "DisplayName est obligatoire.",
            "DSMS005" => "Valeurs acceptées : Custom, Costume, Weapon, Mounts et NPC.",
            "DSMS006" => "« Character » est une ancienne syntaxe ; utilisez « Custom ».",
            "DSMS007" => "Ce type de preset exige un identifiant de personnage ciblé.",
            "DSMS008" => "Seuls les lettres, chiffres, « _ » et « - » sont acceptés.",
            "DSMS010" => $"{issue.Field} est obligatoire.",
            "DSMS011" => "Les presets Custom et Costume exigent le visage en jeu afin de préserver les animations faciales et les morph targets.",
            "DSMS012" => "Aucun Animation Blueprint de physique n’est déclaré ; la physique secondaire du costume peut être absente.",
            "DSMS013" => "Utilisez un chemin d’objet Unreal complet : /Game/.../Asset.Asset (jamais un chemin Windows).",
            "DSMS014" => "Le chemin court a été accepté, mais un chemin d’objet complet reste recommandé.",
            "DSMS015" => "Utilisez un chemin d’Animation Blueprint sous /Game/ avec la classe générée « _C ».",
            "DSMS017" => "Le nom de l’asset du package et le nom de l’objet après le point doivent être strictement identiques.",
            "DSMS018" => "Le nom d’objet de l’Animation Blueprint doit être le nom de l’asset suivi de « _C ».",
            "DSMS020" => "Renseignez WeaponPath, WeaponPaths, ou activez WeaponMaterialsOnly.",
            "DSMS021" => "WeaponPath et WeaponPaths sont présents ensemble ; conservez uniquement la stratégie voulue.",
            "DSMS022" => "WeaponMaterialsOnly exige au moins un remplacement de matériau d’arme.",
            "DSMS030" => "Requirements manquant : le runtime utilisera ['None'].",
            "DSMS031" => "SlotIndex doit être supérieur ou égal à zéro.",
            "DSMS032" => "Un même index de matériau est déclaré plusieurs fois.",
            "DSMS033" => "Le nom du morph target est obligatoire.",
            "DSMS034" => "Un même morph target est déclaré plusieurs fois.",
            "DSMS035" => "DSMS accepte uniquement les valeurs de morph target comprises entre 0,0 et 1,0.",
            "DSMS036" => "Le tableau doit contenir au moins une valeur.",
            "DSMS037" => "La valeur doit être une chaîne non vide.",
            "DSMS038" => "Cette valeur est déclarée plusieurs fois.",
            "DSMS040" => "AuxiliaryMeshPath réutilise BodyPath ; vérifiez qu’un composant existant est remplacé et qu’aucun doublon n’est généré.",
            "DSMS041" => "BodyOutlinePath réutilise BodyPath ; cela n’est valide qu’avec des matériaux d’outline adaptés à cette géométrie.",
            "DSMS042" => "Recette d’outline auxiliaire reconnue : la géométrie de BodyPath est réutilisée avec ses matériaux dédiés tandis que le composant natif est masqué.",
            "DSMS110" => TranslateFModelSuggestion(issue.Message,
                "FModel a trouvé cet asset avec une casse ou une orthographe différente.", "Suggested path:", "Chemin suggéré :"),
            "DSMS111" => TranslateFModelSuggestion(issue.Message,
                "L’asset est absent de l’index des exports FModel actuel.", "Likely match:", "Correspondance probable :"),
            "DSMS112" => TranslateFModelSuggestion(issue.Message,
                "Asset personnalisé absent de l’index FModel actuel : il reste non vérifié et n’est pas considéré comme invalide.", "Likely match:", "Correspondance probable :"),
            "DSMS090" => "Champ inconnu : vérifiez sa prise en charge par le runtime Lua actuel.",
            _ => issue.Message
        };
        return issue with { Message = message };
    }

    private static string TranslateFModelSuggestion(string original, string prefix, string marker, string translatedMarker)
    {
        var index = original.IndexOf(marker, StringComparison.Ordinal);
        return index < 0 ? prefix : $"{prefix} {translatedMarker}{original[(index + marker.Length)..]}";
    }

    public static string Severity(ValidationSeverity severity) =>
        IsFrench(CurrentLanguage) ? severity switch
        {
            ValidationSeverity.Error => "Erreur",
            ValidationSeverity.Warning => "Avertissement",
            _ => "Information"
        } : severity.ToString();
}
