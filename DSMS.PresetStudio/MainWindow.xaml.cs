using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DSMS.Core.Catalog;
using DSMS.Core.Assets;
using DSMS.Core.Models;
using DSMS.Core.Recipes;
using DSMS.Core.Repair;
using DSMS.Core.Repository;
using DSMS.Core.Serialization;
using DSMS.Core.Validation;
using DSMS.PresetStudio.Localization;
using DSMS.PresetStudio.Services;
using DSMS.PresetStudio.Settings;
using DSMS.PresetStudio.Theming;
using Microsoft.Win32;

namespace DSMS.PresetStudio;

public partial class MainWindow : Window
{
    private readonly PresetValidator _validator = new();
    private readonly ObservableCollection<IssueRow> _issues = [];
    private readonly List<IssueRow> _allIssues = [];
    private readonly ObservableCollection<ThemeDefinition> _themes = [];
    private readonly ObservableCollection<MaterialOverride> _bodyMaterials = [];
    private readonly ObservableCollection<MaterialOverride> _faceMaterials = [];
    private readonly ObservableCollection<MaterialOverride> _outlineMaterials = [];
    private readonly ObservableCollection<MaterialOverride> _faceOutlineMaterials = [];
    private readonly ObservableCollection<MaterialOverride> _weaponMaterials = [];
    private PresetDocument _workingPreset = new();
    private string? _currentFile;
    private bool _editorDirty;
    private bool _syncing;
    private bool _syncingSettings;
    private string _issueFilter = "All";
    private string _activeStep = "Identity";
    private readonly AppSettings _settings = SettingsService.Load();
    private readonly CompatibilityProfile _compatibility = CompatibilityService.Load();
    private IconResolution? _iconResolution;
    private StudioUpdate? _availableUpdate;
    private bool _checkingForUpdates;
    private CharacterCatalog _characterCatalog = new([]);
    private FModelAssetIndex? _fmodelIndex;
    private readonly Dictionary<string, string> _recipeEnglishNames = new(StringComparer.OrdinalIgnoreCase);
    private bool _materialsExpanded;

    public MainWindow()
    {
        InitializeComponent();
        IssuesGrid.ItemsSource = _issues;
        BodyMaterialsGrid.ItemsSource = _bodyMaterials;
        FaceMaterialsGrid.ItemsSource = _faceMaterials;
        OutlineMaterialsGrid.ItemsSource = _outlineMaterials;
        FaceOutlineMaterialsGrid.ItemsSource = _faceOutlineMaterials;
        WeaponMaterialsGrid.ItemsSource = _weaponMaterials;
        SetupSettings();
        LoadCatalogs();
        NewPreset();
        ContentRendered += MainWindow_ContentRendered;
    }

    private void SetupSettings()
    {
        foreach (var theme in ThemeManager.BuiltInThemes) _themes.Add(theme);
        foreach (var theme in _settings.CustomThemes.Where(theme =>
                     !ThemeManager.IsSystemThemeName(theme.Name) &&
                     _themes.All(x => !x.Name.Equals(theme.Name, StringComparison.OrdinalIgnoreCase))))
            _themes.Add(theme);
        ThemeCombo.ItemsSource = _themes;
        ThemeCombo.SelectedItem = _themes.FirstOrDefault(x => x.Name.Equals(_settings.ThemeName, StringComparison.OrdinalIgnoreCase)) ?? _themes[0];
        try
        {
            ThemeManager.Apply((ThemeDefinition)ThemeCombo.SelectedItem);
        }
        catch
        {
            ThemeCombo.SelectedItem = _themes[0];
            ThemeManager.Apply(_themes[0]);
            _settings.ThemeName = _themes[0].Name;
            SettingsService.Save(_settings);
        }

        _syncingSettings = true;
        foreach (var item in LanguageCombo.Items.OfType<ComboBoxItem>())
            if (string.Equals(item.Tag as string, _settings.Language, StringComparison.OrdinalIgnoreCase))
                LanguageCombo.SelectedItem = item;
        if (LanguageCombo.SelectedItem is null) LanguageCombo.SelectedIndex = 0;
        StartupUpdateCheckBox.IsChecked = _settings.CheckForUpdatesOnStartup;
        _syncingSettings = false;
        FModelRootBox.Text = _settings.FModelExportRoot;
        FModelIndexStatusText.Text = UiLocalizer.IsFrench(_settings.Language)
            ? "Index FModel non chargé. L’indexation reste manuelle et facultative."
            : "FModel index is not loaded. Indexing remains manual and optional.";
        ModLoaderPathBox.Text = _settings.ModLoaderScriptsPath;
        StudioVersionText.Text = $"v{AppVersion.Current}";
        RefreshModLoaderStatus();
        ApplyLanguageDetails();
        SetUpdateDisplay("READY", Localized("Ready to check", "Prêt à vérifier"), UpdateVisualState.Ready);
    }

    private void LoadCatalogs()
    {
        TypeCombo.ItemsSource = new[] { "Custom", "Costume", "Weapon" };
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        try
        {
            _characterCatalog = CharacterCatalog.Load(Path.Combine(dataDirectory, "characters.json"));
            CharacterCombo.ItemsSource = _characterCatalog.Characters;
            var recipes = RecipeCatalog.Load(Path.Combine(dataDirectory, "recipes.json")).Recipes;
            foreach (var recipe in recipes) _recipeEnglishNames[recipe.Id] = recipe.Name;
            RecipeCombo.ItemsSource = recipes;
            RecipeCombo.SelectedIndex = 0;
            ApplyRecipeLanguage();
        }
        catch (Exception exception)
        {
            StatusText.Text = Localized($"Catalog warning: {exception.Message}", $"Avertissement catalogue : {exception.Message}");
        }
    }

    private void NewPreset_Click(object sender, RoutedEventArgs e) => NewPreset();

    private void NewPreset()
    {
        var preset = new PresetDocument
        {
            Version = 3,
            UniqueID = "author_character_preset",
            DisplayName = "Character - Preset name [Costume Custom]",
            Type = "Costume",
            TargetCharacterID = "CharacterID",
            Requirements = ["None"],
            PhysicsAssetPath = "/Game/Path/Asset_PhysicsAsset.Asset_PhysicsAsset",
            PhysicsAnimBlueprintPath = "/Game/Path/ABP_Physics.ABP_Physics_C",
            FaceMorphPath = "/Game/Path/Character_ingame_face_mesh.Character_ingame_face_mesh",
            BodyPath = "/Game/mods/ModAuthor/ModName/Mesh/Character_body.Character_body",
            BodyClearMaterialOverrides = true,
            BodyMaterials = [new() { SlotIndex = 0, MaterialPath = "/Game/mods/ModAuthor/ModName/Materials/MI_Body.MI_Body" }],
            IconPath = "/Game/Path/Character_Icon.Character_Icon"
        };
        SetWorkingPreset(preset);
        SetActiveStep("Identity");
        _currentFile = null;
        FilePathText.Text = UiLocalizer.IsFrench(_settings.Language) ? "Nouveau preset non enregistré" : "New unsaved preset";
        JsonEditor.Text = PresetSerializer.Serialize(preset);
        _editorDirty = false;
        ValidateEditor();
        RefreshIconPreview();
    }

    private void OpenJson_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = Localized("DSMS JSON presets (*.json)|*.json|All files (*.*)|*.*", "Presets JSON DSMS (*.json)|*.json|Tous les fichiers (*.*)|*.*") };
        if (dialog.ShowDialog(this) != true) return;
        _currentFile = dialog.FileName;
        FilePathText.Text = dialog.FileName;
        JsonEditor.Text = File.ReadAllText(dialog.FileName);
        try { SetWorkingPreset(PresetSerializer.Deserialize(JsonEditor.Text)); }
        catch (JsonException) { }
        _editorDirty = false;
        ValidateEditor();
        RefreshIconPreview();
    }

    private void ScanFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Localized("Select a DSMS preset folder", "Sélectionnez un dossier de presets DSMS"), Multiselect = false };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var result = new PresetRepositoryValidator(_validator).Scan(dialog.FolderName);
            var rows = new List<IssueRow>();
            foreach (var file in result.Files)
                foreach (var issue in file.Report.Issues)
                {
                    var localizedIssue = UiLocalizer.Validation(issue, _settings.Language);
                    rows.Add(new(localizedIssue.Severity, localizedIssue.Code,
                        $"{Path.GetFileName(file.FilePath)} · {localizedIssue.Field}", localizedIssue.Message));
                }
            SetIssues(rows);
            IssueHeader.Text = Localized($"FOLDER RESULTS · {result.FileCount} PRESETS", $"RÉSULTATS DU DOSSIER · {result.FileCount} PRESETS");
            StatusText.Text = Localized(
                $"{result.ValidFileCount}/{result.FileCount} structurally valid · {result.ErrorCount} errors · {result.WarningCount} warnings",
                $"{result.ValidFileCount}/{result.FileCount} structurellement valides · {result.ErrorCount} erreur(s) · {result.WarningCount} avertissement(s)");
            SetRisk(result.ErrorCount, result.WarningCount);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, Localized("Folder scan failed", "Échec de l’analyse du dossier"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GenerateJson_Click(object sender, RoutedEventArgs e)
    {
        UpdateWorkingPresetFromBuilder();
        JsonEditor.Text = PresetSerializer.Serialize(_workingPreset);
        _editorDirty = true;
        WorkspaceTabs.SelectedIndex = 1;
        ValidateEditor();
    }

    private void Validate_Click(object sender, RoutedEventArgs e)
    {
        if (WorkspaceTabs.SelectedIndex == 0)
        {
            UpdateWorkingPresetFromBuilder();
            JsonEditor.Text = PresetSerializer.Serialize(_workingPreset);
        }
        try
        {
            var preset = PresetSerializer.Deserialize(JsonEditor.Text);
            var repairs = new PresetRepairer(_characterCatalog).Repair(preset);
            ApplyRepairResult(preset, repairs);
        }
        catch (JsonException exception)
        {
            ShowJsonError(exception);
        }
    }

    private void ApplyRepairResult(PresetDocument document, IReadOnlyList<RepairChange> changes)
    {
        if (changes.Count > 0)
        {
            _workingPreset = document;
            JsonEditor.Text = PresetSerializer.Serialize(document);
            SetWorkingPreset(document);
            _editorDirty = true;
        }

        ValidateEditor(changes);
        RefreshIconPreview();
    }

    private void Format_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _workingPreset = PresetSerializer.Deserialize(JsonEditor.Text);
            JsonEditor.Text = PresetSerializer.Serialize(_workingPreset);
            SetWorkingPreset(_workingPreset);
            _editorDirty = true;
            ValidateEditor();
        }
        catch (JsonException exception)
        {
            ShowJsonError(exception);
        }
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (WorkspaceTabs.SelectedIndex == 0)
        {
            UpdateWorkingPresetFromBuilder();
            JsonEditor.Text = PresetSerializer.Serialize(_workingPreset);
        }
        PresetDocument preset;
        try { preset = PresetSerializer.Deserialize(JsonEditor.Text); }
        catch (JsonException exception) { ShowJsonError(exception); return; }

        var suggested = string.IsNullOrWhiteSpace(preset.UniqueID) ? "DSMS-New-Preset.json" : $"DSMS-{preset.UniqueID}.json";
        var dialog = new SaveFileDialog { Filter = Localized("DSMS JSON presets (*.json)|*.json", "Presets JSON DSMS (*.json)|*.json"), FileName = suggested };
        if (dialog.ShowDialog(this) != true) return;
        if (!Path.GetFileName(dialog.FileName).StartsWith("DSMS-", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this,
                Localized("The filename must start with DSMS-.", "Le nom du fichier doit commencer par DSMS-."),
                Localized("Unsafe filename", "Nom de fichier non sécurisé"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        File.WriteAllText(dialog.FileName, PresetSerializer.Serialize(preset));
        _currentFile = dialog.FileName;
        FilePathText.Text = dialog.FileName;
        _editorDirty = false;
        ValidateEditor();
    }

    private void ValidateEditor(IReadOnlyList<RepairChange>? repairs = null)
    {
        try
        {
            var preset = PresetSerializer.Deserialize(JsonEditor.Text);
            var report = _validator.Validate(preset, _currentFile is null ? "DSMS-New-Preset.json" : Path.GetFileName(_currentFile));
            if (_fmodelIndex is not null)
                report.Issues.AddRange(new FModelPresetValidator(_fmodelIndex).Validate(preset).Issues);
            var rows = new List<IssueRow>();
            if (repairs is { Count: > 0 })
                rows.AddRange(repairs.Select(x => new IssueRow(ValidationSeverity.Info, "FIX", x.Field,
                    UiLocalizer.IsFrench(_settings.Language) ? $"Correction sûre appliquée : {TranslateRepair(x.Message)}" : $"Safe repair applied: {x.Message}")));
            rows.AddRange(report.Issues.Select(issue => UiLocalizer.Validation(issue, _settings.Language))
                .Select(issue => new IssueRow(issue.Severity, issue.Code, issue.Field, issue.Message)));
            SetIssues(rows);
            IssueHeader.Text = UiLocalizer.IsFrench(_settings.Language) ? "RÉSULTATS DE VALIDATION" : "VALIDATION RESULTS";
            StatusText.Text = UiLocalizer.IsFrench(_settings.Language)
                ? $"{LocalizeRiskLevel(report.RiskLevel)} · {report.ErrorCount} erreur(s) · {report.WarningCount} avertissement(s)" + (_editorDirty ? " · modifications non enregistrées" : "")
                : $"{report.RiskLevel.ToUpperInvariant()} · {report.ErrorCount} errors · {report.WarningCount} warnings" + (_editorDirty ? " · unsaved changes" : "");
            SetRisk(report.ErrorCount, report.WarningCount);
        }
        catch (JsonException exception)
        {
            SetIssues([new(ValidationSeverity.Error, "JSON", "Document", exception.Message)]);
            StatusText.Text = UiLocalizer.IsFrench(_settings.Language) ? "ROUGE · JSON invalide" : "RED · Invalid JSON";
            SetRisk(1, 0);
        }
    }

    private void SetWorkingPreset(PresetDocument preset)
    {
        _syncing = true;
        _workingPreset = preset;
        TypeCombo.SelectedItem = preset.Type ?? "Costume";
        TargetIdBox.Text = preset.TargetCharacterID ?? "";
        UniqueIdBox.Text = preset.UniqueID ?? "";
        DisplayNameBox.Text = preset.DisplayName ?? "";
        BodyPathBox.Text = preset.BodyPath ?? "";
        FaceMorphPathBox.Text = preset.FaceMorphPath ?? "";
        FacePathBox.Text = preset.FacePath ?? "";
        PhysicsAssetPathBox.Text = preset.PhysicsAssetPath ?? "";
        PhysicsAbpPathBox.Text = preset.PhysicsAnimBlueprintPath ?? "";
        IconPathBox.Text = preset.IconPath ?? "";
        BodyOutlinePathBox.Text = preset.BodyOutlinePath ?? "";
        FaceOutlinePathBox.Text = preset.FaceOutlinePath ?? "";
        AuxiliaryMeshPathBox.Text = preset.AuxiliaryMeshPath ?? "";
        WeaponPathBox.Text = preset.WeaponPath ?? "";
        RequirementsBox.Text = string.Join(", ", preset.Requirements ?? ["None"]);
        BodyClearCheck.IsChecked = preset.BodyClearMaterialOverrides == true;
        FaceClearCheck.IsChecked = preset.FaceClearMaterialOverrides == true;
        OutlineClearCheck.IsChecked = preset.BodyOutlineClearMaterialOverrides == true;
        FaceOutlineClearCheck.IsChecked = preset.FaceOutlineClearMaterialOverrides == true;
        WeaponClearCheck.IsChecked = preset.WeaponClearMaterialOverrides == true;
        WeaponMaterialsOnlyCheck.IsChecked = preset.WeaponMaterialsOnly == true;
        ReplaceCollection(_bodyMaterials, preset.BodyMaterials);
        ReplaceCollection(_faceMaterials, preset.FaceMaterials);
        ReplaceCollection(_outlineMaterials, preset.BodyOutlineMaterials);
        ReplaceCollection(_faceOutlineMaterials, preset.FaceOutlineMaterials);
        ReplaceCollection(_weaponMaterials, preset.WeaponMaterials);

        CharacterCombo.SelectedItem = _characterCatalog.Find(preset.TargetCharacterID);
        SelectRecipeForPreset(preset);
        _syncing = false;
        RefreshIconPreview();
    }

    private void SelectRecipeForPreset(PresetDocument preset)
    {
        if (RecipeCombo.ItemsSource is not IEnumerable<PresetRecipe> recipes) return;

        var recipeId = InferRecipeId(preset);
        RecipeCombo.SelectedItem = recipes.FirstOrDefault(x => x.Id.Equals(recipeId, StringComparison.OrdinalIgnoreCase))
                                   ?? recipes.FirstOrDefault(x => x.Type.Equals(preset.Type, StringComparison.OrdinalIgnoreCase));
    }

    private static string InferRecipeId(PresetDocument preset)
    {
        if (preset.Type?.Equals("Weapon", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (preset.WeaponMaterialsOnly == true) return "weapon-retexture";
            if (preset.WeaponPaths is { Count: > 0 }) return "weapon-multi";
            return "weapon-single";
        }

        if (preset.Type?.Equals("Custom", StringComparison.OrdinalIgnoreCase) == true &&
            !string.IsNullOrWhiteSpace(preset.BodyOutlinePath))
            return "custom-outline";

        if (preset.HiddenComponentMeshMatches is { Count: > 0 } ||
            !string.IsNullOrWhiteSpace(preset.AuxiliaryMeshPath))
            return "auxiliary-hide";

        if (!string.IsNullOrWhiteSpace(preset.FaceMorphPath) ||
            !string.IsNullOrWhiteSpace(preset.PhysicsAssetPath) ||
            !string.IsNullOrWhiteSpace(preset.PhysicsAnimBlueprintPath))
            return "costume-full";

        return "costume-retexture";
    }

    private void UpdateWorkingPresetFromBuilder()
    {
        CommitMaterialEdits();
        _workingPreset.Version = 3;
        _workingPreset.UniqueID = NullIfBlank(UniqueIdBox.Text);
        _workingPreset.DisplayName = NullIfBlank(DisplayNameBox.Text);
        _workingPreset.Type = TypeCombo.SelectedItem as string ?? "Costume";
        _workingPreset.TargetCharacterID = NullIfBlank(TargetIdBox.Text);
        _workingPreset.Requirements = RequirementsBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (_workingPreset.Requirements.Count == 0) _workingPreset.Requirements = ["None"];
        _workingPreset.BodyPath = NullIfBlank(BodyPathBox.Text);
        _workingPreset.FaceMorphPath = NullIfBlank(FaceMorphPathBox.Text);
        _workingPreset.FacePath = NullIfBlank(FacePathBox.Text);
        _workingPreset.PhysicsAssetPath = NullIfBlank(PhysicsAssetPathBox.Text);
        _workingPreset.PhysicsAnimBlueprintPath = NullIfBlank(PhysicsAbpPathBox.Text);
        _workingPreset.IconPath = NullIfBlank(IconPathBox.Text);
        _workingPreset.BodyOutlinePath = NullIfBlank(BodyOutlinePathBox.Text);
        _workingPreset.FaceOutlinePath = NullIfBlank(FaceOutlinePathBox.Text);
        _workingPreset.AuxiliaryMeshPath = NullIfBlank(AuxiliaryMeshPathBox.Text);
        _workingPreset.WeaponPath = NullIfBlank(WeaponPathBox.Text);
        _workingPreset.BodyClearMaterialOverrides = BodyClearCheck.IsChecked == true ? true : null;
        _workingPreset.FaceClearMaterialOverrides = FaceClearCheck.IsChecked == true ? true : null;
        _workingPreset.BodyOutlineClearMaterialOverrides = OutlineClearCheck.IsChecked == true ? true : null;
        _workingPreset.FaceOutlineClearMaterialOverrides = FaceOutlineClearCheck.IsChecked == true ? true : null;
        _workingPreset.WeaponClearMaterialOverrides = WeaponClearCheck.IsChecked == true ? true : null;
        _workingPreset.WeaponMaterialsOnly = WeaponMaterialsOnlyCheck.IsChecked == true ? true : null;
        _workingPreset.BodyMaterials = _bodyMaterials.Count == 0 ? null : _bodyMaterials.ToList();
        _workingPreset.FaceMaterials = _faceMaterials.Count == 0 ? null : _faceMaterials.ToList();
        _workingPreset.BodyOutlineMaterials = _outlineMaterials.Count == 0 ? null : _outlineMaterials.ToList();
        _workingPreset.FaceOutlineMaterials = _faceOutlineMaterials.Count == 0 ? null : _faceOutlineMaterials.ToList();
        _workingPreset.WeaponMaterials = _weaponMaterials.Count == 0 ? null : _weaponMaterials.ToList();

        if (_workingPreset.Type.Equals("Weapon", StringComparison.OrdinalIgnoreCase))
        {
            _workingPreset.BodyPath = null;
            _workingPreset.FaceMorphPath = null;
            _workingPreset.FacePath = null;
            _workingPreset.PhysicsAssetPath = null;
            _workingPreset.PhysicsAnimBlueprintPath = null;
            _workingPreset.BodyOutlinePath = null;
            _workingPreset.FaceOutlinePath = null;
            _workingPreset.AuxiliaryMeshPath = null;
            _workingPreset.BodyMaterials = null;
            _workingPreset.FaceMaterials = null;
            _workingPreset.BodyOutlineMaterials = null;
            _workingPreset.FaceOutlineMaterials = null;
            _workingPreset.BodyClearMaterialOverrides = null;
            _workingPreset.FaceClearMaterialOverrides = null;
            _workingPreset.BodyOutlineClearMaterialOverrides = null;
            _workingPreset.FaceOutlineClearMaterialOverrides = null;
        }
        else
        {
            _workingPreset.WeaponPath = null;
            _workingPreset.WeaponPaths = null;
            _workingPreset.WeaponMaterials = null;
            _workingPreset.WeaponMaterialsOnly = null;
            _workingPreset.WeaponClearMaterialOverrides = null;
        }
    }

    private void CommitMaterialEdits()
    {
        foreach (var grid in new[] { BodyMaterialsGrid, FaceMaterialsGrid, OutlineMaterialsGrid, FaceOutlineMaterialsGrid, WeaponMaterialsGrid })
        {
            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.CommitEdit(DataGridEditingUnit.Row, true);
        }
    }

    private static void ReplaceCollection(ObservableCollection<MaterialOverride> destination, IEnumerable<MaterialOverride>? source)
    {
        destination.Clear();
        if (source is null) return;
        foreach (var item in source)
            destination.Add(new MaterialOverride { SlotIndex = item.SlotIndex, MaterialMatch = item.MaterialMatch, MaterialPath = item.MaterialPath });
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void CharacterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || CharacterCombo.SelectedItem is not CharacterDefinition character) return;
        TargetIdBox.Text = character.InternalId;
    }

    private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || !IsLoaded) return;
        RefreshIconPreview();
    }

    private void RecipeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecipeCombo.SelectedItem is not PresetRecipe recipe) return;
        RefreshRecipeText(recipe);
        if (!_syncing) TypeCombo.SelectedItem = recipe.Type;
    }

    private void RefreshRecipeText(PresetRecipe recipe)
    {
        RecipeNameText.Text = UiLocalizer.RecipeName(recipe.Id, _recipeEnglishNames.GetValueOrDefault(recipe.Id, recipe.Name), _settings.Language);
        RecipeDescriptionText.Text = LocalizedRecipeDescription(recipe);
        RecipeFieldsText.Text = UiLocalizer.IsFrench(_settings.Language)
            ? $"Obligatoire : {string.Join(", ", recipe.RequiredFields)}\nRecommandé : {string.Join(", ", recipe.RecommendedFields)}"
            : $"Required: {string.Join(", ", recipe.RequiredFields)}\nRecommended: {string.Join(", ", recipe.RecommendedFields)}";
    }

    private void ExpandMaterials_Click(object sender, RoutedEventArgs e)
    {
        _materialsExpanded = !_materialsExpanded;
        BuilderScrollViewer.Visibility = _materialsExpanded ? Visibility.Collapsed : Visibility.Visible;
        Grid.SetColumn(DetailsScrollViewer, _materialsExpanded ? 2 : 4);
        Grid.SetColumnSpan(DetailsScrollViewer, _materialsExpanded ? 3 : 1);
        ExpandMaterialsButton.Content = UiLocalizer.Text(_materialsExpanded ? "Restore layout" : "Expand materials", _settings.Language);
        MaterialsSection.BringIntoView();
    }

    private string LocalizedRecipeDescription(PresetRecipe recipe)
    {
        if (!_settings.Language.Equals("fr-FR", StringComparison.OrdinalIgnoreCase)) return recipe.Description;
        return recipe.Id switch
        {
            "costume-full" => "Remplace le corps, le visage et les matériaux d’un personnage jouable tout en conservant ses animations et sa physique de costume.",
            "costume-retexture" => "Conserve le Skeletal Mesh du corps et remplace uniquement ses Material Instances.",
            "weapon-single" => "Remplace un mesh d’arme unique et ses Material Instances.",
            "weapon-multi" => "Remplace plusieurs composants d’arme à l’aide de sélecteurs ComponentMatch.",
            "weapon-retexture" => "Conserve le mesh d’arme natif et remplace uniquement ses Material Instances.",
            "custom-outline" => "Charge un corps personnalisé avec un Skeletal Mesh parallèle pour l’outline du corps et un outline du visage facultatif.",
            "auxiliary-hide" => "Masque ou remplace les composants de costume natifs séparés du corps principal.",
            _ => recipe.Description
        };
    }

    private void StepButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string step }) return;
        SetActiveStep(step);

        Dispatcher.BeginInvoke(() =>
        {
            switch (step)
            {
                case "Body":
                    BodySection.BringIntoView();
                    break;
                case "Materials":
                    MaterialsSection.BringIntoView();
                    break;
                case "Advanced":
                    AdvancedSection.BringIntoView();
                    OptionsSection.BringIntoView();
                    break;
                default:
                    IdentitySection.BringIntoView();
                    DetailsScrollViewer.ScrollToTop();
                    break;
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void SetActiveStep(string activeStep)
    {
        _activeStep = activeStep;
        var primaryStyle = (Style)FindResource("PrimaryButtonStyle");
        var defaultStyle = (Style)FindResource(typeof(Button));
        var primaryBrush = (Brush)FindResource("PrimaryBrush");
        var borderBrush = (Brush)FindResource("BorderBrush");
        var primaryText = new SolidColorBrush(Color.FromRgb(7, 17, 24));
        primaryText.Freeze();
        var defaultText = (Brush)FindResource("TextPrimaryBrush");

        var steps = new (string Name, Button Button, Border Badge)[]
        {
            ("Identity", IdentityStepButton, IdentityStepBadge),
            ("Body", BodyStepButton, BodyStepBadge),
            ("Materials", MaterialsStepButton, MaterialsStepBadge),
            ("Advanced", AdvancedStepButton, AdvancedStepBadge)
        };

        foreach (var item in steps)
        {
            var isActive = item.Name.Equals(activeStep, StringComparison.OrdinalIgnoreCase);
            item.Button.Style = isActive ? primaryStyle : defaultStyle;
            item.Badge.Background = isActive ? primaryBrush : borderBrush;
            if (item.Badge.Child is TextBlock label) label.Foreground = isActive ? primaryText : defaultText;
        }
    }

    private void FilterIssues_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string filter })
        {
            _issueFilter = filter;
            RefreshIssueFilter();
        }
    }

    private void SetIssues(IEnumerable<IssueRow> rows)
    {
        _allIssues.Clear();
        _allIssues.AddRange(rows);
        RefreshIssueFilter();
    }

    private void RefreshIssueFilter()
    {
        _issues.Clear();
        foreach (var row in _allIssues.Where(x => _issueFilter == "All" || x.Severity.ToString().Equals(_issueFilter, StringComparison.OrdinalIgnoreCase)))
            _issues.Add(row);
        AllCountText.Text = _allIssues.Count.ToString();
        ErrorCountText.Text = _allIssues.Count(x => x.Severity == ValidationSeverity.Error).ToString();
        WarningCountText.Text = _allIssues.Count(x => x.Severity == ValidationSeverity.Warning).ToString();
        InfoCountText.Text = _allIssues.Count(x => x.Severity == ValidationSeverity.Info).ToString();
        NoIssuesPanel.Visibility = _issues.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyTheme_Click(object sender, RoutedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is not ThemeDefinition theme) return;
        try
        {
            ThemeManager.Apply(theme);
            SetActiveStep(_activeStep);
            JsonEditor.RefreshHighlighting();
            _settings.ThemeName = theme.Name;
            if (!theme.IsOfficialSystemTheme) StoreCustomTheme(theme);
            SettingsService.Save(_settings);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, LocalizeThemeError(exception.Message), Localized("Invalid theme", "Thème invalide"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportTheme_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = Localized("DSMS theme (*.json)|*.json|JSON files (*.json)|*.json", "Thème DSMS (*.json)|*.json|Fichiers JSON (*.json)|*.json") };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var theme = ThemeManager.LoadFromFile(dialog.FileName);
            var existing = _themes.FirstOrDefault(x => x.Name.Equals(theme.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is not null) _themes.Remove(existing);
            _themes.Add(theme);
            ThemeCombo.SelectedItem = theme;
            ThemeManager.Apply(theme);
            SetActiveStep(_activeStep);
            JsonEditor.RefreshHighlighting();
            _settings.ThemeName = theme.Name;
            StoreCustomTheme(theme);
            SettingsService.Save(_settings);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, LocalizeThemeError(exception.Message), Localized("Theme import failed", "Échec de l’importation du thème"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenThemeDesigner_Click(object sender, RoutedEventArgs e)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Tools", "ThemeDesigner", "index.html");
        if (!File.Exists(filePath))
        {
            MessageBox.Show(this,
                Localized("Theme Designer was not found in the application folder.", "Theme Designer est introuvable dans le dossier de l’application."),
                Localized("Missing tool", "Outil manquant"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var selected = ThemeCombo.SelectedItem as ThemeDefinition;
            var library = _themes.Select(theme => new
            {
                theme.Name, theme.Author, theme.Background, theme.Panel, theme.PanelAlt, theme.Input, theme.Border,
                theme.Primary, theme.Secondary, theme.TextPrimary, theme.TextSecondary, theme.Success, theme.Warning, theme.Error,
                theme.FontFamily, theme.FontSize, theme.UiOpacity, theme.TextOpacity,
                theme.BackgroundImageBase64, theme.BackgroundImageOpacity,
                Official = theme.IsOfficialSystemTheme,
                Selected = ReferenceEquals(theme, selected)
            }).ToArray();
            var marker = "/*__DSMS_THEME_LIBRARY__*/ null";
            var source = File.ReadAllText(filePath);
            if (!source.Contains(marker, StringComparison.Ordinal))
                throw new InvalidDataException(Localized("Theme Designer does not contain the theme-library marker.", "Theme Designer ne contient pas le marqueur de bibliothèque de thèmes."));
            source = source.Replace(marker, JsonSerializer.Serialize(library), StringComparison.Ordinal);
            var sessionDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HoverModsVault", "DSMSPresetStudio", "ThemeDesigner");
            Directory.CreateDirectory(sessionDirectory);
            var sessionPath = Path.Combine(sessionDirectory, "index.html");
            File.WriteAllText(sessionPath, source);
            Process.Start(new ProcessStartInfo(sessionPath) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, LocalizeThemeError(exception.Message), Localized("Theme Designer failed", "Échec de Theme Designer"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateThemeIdentity();

    private void UpdateThemeIdentity()
    {
        if (ThemeCombo.SelectedItem is not ThemeDefinition theme) return;
        var french = UiLocalizer.IsFrench(_settings.Language);
        ThemeIdentityText.Text = theme.IsOfficialSystemTheme
            ? $"{ThemeManager.OfficialSystemAuthor} · Official Theme System · {(french ? "lecture seule" : "read-only")}"
            : $"{theme.Author} · {(french ? "Thème personnalisé" : "Custom Theme")}";
    }

    private void StoreCustomTheme(ThemeDefinition theme)
    {
        var index = _settings.CustomThemes.FindIndex(x => x.Name.Equals(theme.Name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0) _settings.CustomThemes[index] = theme;
        else _settings.CustomThemes.Add(theme);
        _settings.CustomTheme = null;
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingSettings || LanguageCombo.SelectedItem is not ComboBoxItem item) return;
        _settings.Language = item.Tag as string ?? "en-GB";
        SettingsService.Save(_settings);
        ApplyLanguageDetails();
    }

    private void ApplyLanguageDetails()
    {
        var updateBadge = UpdateBadgeText.Text;
        var updateStatus = UpdateStatusText.Text;
        UiLocalizer.Apply(this, _settings.Language);
        UpdateBadgeText.Text = updateBadge;
        UpdateStatusText.Text = updateStatus;
        SettingsDescriptionText.Text = DetailLocalizer.Get("SettingsIntro", _settings.Language);
        ThemeHelpText.Text = DetailLocalizer.Get("ThemeHelp", _settings.Language);
        LanguageHelpText.Text = DetailLocalizer.Get("LanguageHelp", _settings.Language);
        AboutDescriptionText.Text = DetailLocalizer.Get("About", _settings.Language);
        UpdateDescriptionText.Text = DetailLocalizer.Get("Updates", _settings.Language);
        MaterialHintText.Text = DetailLocalizer.Get("MaterialHint", _settings.Language);
        NoIssuesText.Text = DetailLocalizer.Get("NoIssues", _settings.Language);
        ApplyRecipeLanguage();
        LocalizeGridHeaders();
        if (RecipeCombo.SelectedItem is PresetRecipe recipe) RefreshRecipeText(recipe);
        ExpandMaterialsButton.Content = UiLocalizer.Text(_materialsExpanded ? "Restore layout" : "Expand materials", _settings.Language);
        RefreshIconPreview();
        RefreshModLoaderStatus();
        UpdateThemeIdentity();
        if (!string.IsNullOrWhiteSpace(JsonEditor.Text)) ValidateEditor();
    }

    private void ApplyRecipeLanguage()
    {
        if (RecipeCombo.ItemsSource is not IEnumerable<PresetRecipe> recipes) return;
        foreach (var recipe in recipes)
        {
            var english = _recipeEnglishNames.GetValueOrDefault(recipe.Id, recipe.Name);
            recipe.Name = UiLocalizer.RecipeName(recipe.Id, english, _settings.Language);
        }
        RecipeCombo.Items.Refresh();
    }

    private void LocalizeGridHeaders()
    {
        foreach (var grid in new[] { BodyMaterialsGrid, FaceMaterialsGrid, OutlineMaterialsGrid, FaceOutlineMaterialsGrid })
        {
            grid.Columns[0].Header = UiLocalizer.Text("Slot", _settings.Language);
            grid.Columns[1].Header = UiLocalizer.Text("Material instance path", _settings.Language);
        }
        WeaponMaterialsGrid.Columns[0].Header = UiLocalizer.Text("Slot", _settings.Language);
        WeaponMaterialsGrid.Columns[1].Header = UiLocalizer.Text("Current material match (optional)", _settings.Language);
        WeaponMaterialsGrid.Columns[2].Header = UiLocalizer.Text("Material instance path", _settings.Language);
        IssuesGrid.Columns[0].Header = UiLocalizer.Text("Level", _settings.Language);
        IssuesGrid.Columns[1].Header = "Code";
        IssuesGrid.Columns[2].Header = UiLocalizer.Text("File / Field", _settings.Language);
        IssuesGrid.Columns[3].Header = UiLocalizer.Text("Message", _settings.Language);
    }

    private string TranslateRepair(string message)
    {
        if (message.StartsWith("Set the active", StringComparison.Ordinal)) return "version du schéma définie sur 3.";
        if (message.StartsWith("Resolved '", StringComparison.Ordinal)) return message.Replace("Resolved", "résolu").Replace("to the canonical character ID", "vers l’identifiant canonique");
        if (message.StartsWith("Normalized the Unreal", StringComparison.Ordinal)) return message.Replace("Normalized the Unreal object path to", "chemin d’objet Unreal normalisé vers");
        if (message.StartsWith("Normalized the Animation", StringComparison.Ordinal)) return message.Replace("Normalized the Animation Blueprint class path to", "chemin de classe Animation Blueprint normalisé vers");
        if (message.StartsWith("Removed exact duplicates", StringComparison.Ordinal)) return "doublons exacts supprimés et slots de matériaux triés.";
        if (message.StartsWith("Removed", StringComparison.Ordinal)) return "valeurs vides ou dupliquées supprimées et espaces normalisés.";
        if (message.StartsWith("Added", StringComparison.Ordinal)) return "valeur par défaut « None » ajoutée.";
        if (message.StartsWith("Replaced", StringComparison.Ordinal)) return "ancien type « Character » remplacé par « Custom ».";
        if (message.StartsWith("Restored '", StringComparison.Ordinal)) return message.Replace("Restored", "restauré").Replace("from the validated preset reference database", "depuis la base de référence des presets validés");
        if (message.StartsWith("Recovered the malformed", StringComparison.Ordinal)) return "JSON syntaxiquement endommagé reconstruit depuis la base de référence des presets validés.";
        return message;
    }

    private void IconPathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncing || !IsLoaded) return;
        _workingPreset.IconPath = NullIfBlank(IconPathBox.Text);
        RefreshIconPreview();
    }

    private void RefreshIconPreview()
    {
        if (IconPreviewImage is null) return;
        _workingPreset.IconPath = NullIfBlank(IconPathBox.Text);
        _workingPreset.Type = TypeCombo.SelectedItem as string ?? _workingPreset.Type;
        _workingPreset.TargetCharacterID = NullIfBlank(TargetIdBox.Text);
        _workingPreset.UniqueID = NullIfBlank(UniqueIdBox.Text);
        _workingPreset.DisplayName = NullIfBlank(DisplayNameBox.Text);
        _iconResolution = IconResolver.Resolve(_workingPreset, _settings.FModelExportRoot);
        IconSourceText.Text = LocalizeIconSource(_iconResolution.Source);
        IconStatusText.Text = LocalizeIconMessage(_iconResolution.Message);
        ApplySuggestedIconButton.Visibility = _iconResolution.HasSuggestion ? Visibility.Visible : Visibility.Collapsed;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(_iconResolution.FilePath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            IconPreviewImage.Source = image;
        }
        catch { IconPreviewImage.Source = null; }
    }

    private void ImportIcon_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = Localized("Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All files|*.*", "Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Tous les fichiers|*.*") };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var iconPath = IconPathBox.Text.Trim();
            var assetName = iconPath.Contains('/') ? iconPath[(iconPath.LastIndexOf('/') + 1)..].Split('.')[0] : null;
            var imported = IconResolver.Import(dialog.FileName, assetName);
            IconStatusText.Text = Localized($"Imported to {imported}.", $"Importée dans {imported}.");
            RefreshIconPreview();
        }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, Localized("Icon import failed", "Échec de l’importation de l’icône"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ApplySuggestedIcon_Click(object sender, RoutedEventArgs e)
    {
        if (_iconResolution?.SuggestedIconPath is not { } suggested) return;
        IconPathBox.Text = suggested;
        RefreshIconPreview();
    }

    private void BrowseFModelRoot_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Localized("Select the FModel DS/Content export folder", "Sélectionnez le dossier d’export FModel DS/Content"), Multiselect = false };
        if (dialog.ShowDialog(this) != true) return;
        FModelRootBox.Text = dialog.FolderName;
        _settings.FModelExportRoot = dialog.FolderName;
        _fmodelIndex = null;
        SettingsService.Save(_settings);
        RefreshIconPreview();
    }

    private async void IndexFModel_Click(object sender, RoutedEventArgs e)
    {
        _settings.FModelExportRoot = FModelRootBox.Text.Trim();
        SettingsService.Save(_settings);
        IndexFModelButton.IsEnabled = false;
        FModelIndexStatusText.Text = UiLocalizer.IsFrench(_settings.Language)
            ? "Indexation des exports FModel en arrière-plan…"
            : "Indexing FModel exports in the background…";
        try
        {
            _fmodelIndex = await Task.Run(() => FModelAssetIndex.Build(_settings.FModelExportRoot));
            FModelIndexStatusText.Text = UiLocalizer.IsFrench(_settings.Language)
                ? $"{_fmodelIndex.Count:N0} assets indexés. Les chemins seront vérifiés lors de la validation."
                : $"{_fmodelIndex.Count:N0} assets indexed. Paths will be checked during validation.";
            ValidateEditor();
        }
        catch (Exception exception)
        {
            _fmodelIndex = null;
            FModelIndexStatusText.Text = exception.Message;
            MessageBox.Show(this, exception.Message,
                UiLocalizer.IsFrench(_settings.Language) ? "Échec de l’indexation FModel" : "FModel indexing failed",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { IndexFModelButton.IsEnabled = true; }
    }

    private void BrowseModLoader_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Localized("Select HMVDSMeshSelector or its Scripts folder", "Sélectionnez HMVDSMeshSelector ou son dossier Scripts"), Multiselect = false };
        if (dialog.ShowDialog(this) != true) return;
        _settings.ModLoaderScriptsPath = CompatibilityService.NormalizeScriptsPath(dialog.FolderName);
        ModLoaderPathBox.Text = _settings.ModLoaderScriptsPath;
        SettingsService.Save(_settings);
        RefreshModLoaderStatus();
    }

    private void AssetPaths_LostFocus(object sender, RoutedEventArgs e)
    {
        _settings.FModelExportRoot = FModelRootBox.Text.Trim();
        if (_fmodelIndex is not null && !_fmodelIndex.Root.Equals(_settings.FModelExportRoot, StringComparison.OrdinalIgnoreCase))
            _fmodelIndex = null;
        _settings.ModLoaderScriptsPath = ModLoaderPathBox.Text.Trim();
        SettingsService.Save(_settings);
        RefreshIconPreview();
    }

    private void DetectModLoader_Click(object sender, RoutedEventArgs e)
    {
        _settings.ModLoaderScriptsPath = ModLoaderPathBox.Text.Trim();
        SettingsService.Save(_settings);
        RefreshModLoaderStatus();
    }

    private void RefreshModLoaderStatus()
    {
        var status = CompatibilityService.Detect(_settings.ModLoaderScriptsPath, _compatibility);
        if (status.ScriptsPath is not null && !status.ScriptsPath.Equals(_settings.ModLoaderScriptsPath, StringComparison.OrdinalIgnoreCase))
        {
            _settings.ModLoaderScriptsPath = status.ScriptsPath;
            ModLoaderPathBox.Text = status.ScriptsPath;
            SettingsService.Save(_settings);
        }
        var state = LocalizeModLoaderState(status.State);
        ModLoaderStatusText.Text = $"{state} — {LocalizeModLoaderMessage(status)}";
        ModLoaderVersionText.Text = status.Version is null
            ? (UiLocalizer.IsFrench(_settings.Language) ? $"Introuvable · version recommandée v{_compatibility.RecommendedModLoaderVersion}" : $"Not found · recommended v{_compatibility.RecommendedModLoaderVersion}")
            : $"v{status.Version} · {state}";
        HeaderSubtitleText.Text = status.Version is null
            ? Localized(
                $"DragonSword: Awakening · DSMS ModLoader v{_compatibility.RecommendedModLoaderVersion} recommended · JSON v3",
                $"DragonSword: Awakening · DSMS ModLoader v{_compatibility.RecommendedModLoaderVersion} recommandé · JSON v3")
            : $"DragonSword: Awakening · DSMS ModLoader v{status.Version} · JSON v3";
    }

    private void OpenIconsFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(IconResolver.ImportedDirectory);
        Process.Start(new ProcessStartInfo(IconResolver.IconsDirectory) { UseShellExecute = true });
    }

    private enum UpdateVisualState { Ready, Checking, Current, Error }

    private async void MainWindow_ContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= MainWindow_ContentRendered;
        if (_settings.CheckForUpdatesOnStartup) await CheckForUpdatesAsync(showPrompt: true);
    }

    private void StartupUpdateCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_syncingSettings) return;
        _settings.CheckForUpdatesOnStartup = StartupUpdateCheckBox.IsChecked == true;
        SettingsService.Save(_settings);
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e) =>
        await CheckForUpdatesAsync(showPrompt: false);

    private async Task CheckForUpdatesAsync(bool showPrompt)
    {
        if (_checkingForUpdates) return;
        _checkingForUpdates = true;
        CheckUpdatesButton.IsEnabled = false;
        InstallUpdateButton.IsEnabled = false;
        SetUpdateDisplay("CHECKING", Localized("Contacting GitHub…", "Connexion à GitHub…"), UpdateVisualState.Checking);
        try
        {
            _availableUpdate = await UpdateService.CheckAsync(_compatibility.StudioReleaseRepository);
            if (_availableUpdate is null)
            {
                SetUpdateDisplay("NO PACKAGE",
                    Localized("The latest release has no compatible ZIP package.", "La dernière version ne contient aucun package ZIP compatible."),
                    UpdateVisualState.Error);
            }
            else if (_availableUpdate.IsNewer)
            {
                SetUpdateDisplay("AVAILABLE",
                    Localized($"Studio v{_availableUpdate.Version} is available. SHA-256 verification is required before installation.",
                              $"Studio v{_availableUpdate.Version} est disponible. La vérification SHA-256 est obligatoire avant l’installation."),
                    UpdateVisualState.Ready);
                InstallUpdateButton.IsEnabled = true;
                if (showPrompt) await PromptAndInstallUpdateAsync();
            }
            else
            {
                SetUpdateDisplay("CURRENT",
                    Localized($"DSMS Preset Studio v{AppVersion.Current} is up to date.", $"DSMS Preset Studio v{AppVersion.Current} est à jour."),
                    UpdateVisualState.Current);
            }
        }
        catch (Exception exception)
        {
            SetUpdateDisplay("FAILED", LocalizeUpdateMessage(exception.Message), UpdateVisualState.Error);
        }
        finally
        {
            _checkingForUpdates = false;
            CheckUpdatesButton.IsEnabled = true;
        }
    }

    private async void InstallUpdate_Click(object sender, RoutedEventArgs e) => await PromptAndInstallUpdateAsync();

    private async Task PromptAndInstallUpdateAsync()
    {
        if (_availableUpdate is null) return;
        var prompt = new UpdatePromptWindow(_availableUpdate, _settings.Language) { Owner = this };
        if (prompt.ShowDialog() == true) await InstallAvailableUpdateAsync();
    }

    private async Task InstallAvailableUpdateAsync()
    {
        if (_availableUpdate is null) return;
        CheckUpdatesButton.IsEnabled = false;
        InstallUpdateButton.IsEnabled = false;
        try
        {
            SetUpdateDisplay("CHECKING", Localized("Preparing download…", "Préparation du téléchargement…"), UpdateVisualState.Checking);
            var progress = new Progress<string>(message =>
                SetUpdateDisplay("CHECKING", LocalizeUpdateMessage(message), UpdateVisualState.Checking));
            var prepared = await UpdateService.DownloadAsync(_availableUpdate, progress);
            SetUpdateDisplay("CHECKING", Localized("Verified. Restarting into the updater…", "Package vérifié. Redémarrage vers la mise à jour…"), UpdateVisualState.Checking);
            UpdateService.LaunchInstaller(prepared);
            Application.Current.Shutdown();
        }
        catch (Exception exception)
        {
            var message = LocalizeUpdateMessage(exception.Message);
            SetUpdateDisplay("FAILED", message, UpdateVisualState.Error);
            MessageBox.Show(this, message, Localized("Update failed", "Échec de la mise à jour"), MessageBoxButton.OK, MessageBoxImage.Error);
            CheckUpdatesButton.IsEnabled = true;
            InstallUpdateButton.IsEnabled = true;
        }
    }

    private void SetUpdateDisplay(string badge, string message, UpdateVisualState state)
    {
        UpdateBadgeText.Text = badge;
        UpdateStatusText.Text = message;
        var brushKey = state switch
        {
            UpdateVisualState.Ready => "UpdateReadyBrush",
            UpdateVisualState.Checking => "UpdateCheckingBrush",
            UpdateVisualState.Error => "UpdateErrorBrush",
            _ => "PrimaryBrush"
        };
        UpdateBadgeText.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
        UpdateBadge.SetResourceReference(Border.BorderBrushProperty, brushKey);
        UpdateStatusText.SetResourceReference(TextBlock.ForegroundProperty,
            state == UpdateVisualState.Error ? "UpdateErrorBrush" : "TextSecondaryBrush");
    }

    private string Localized(string english, string french) =>
        UiLocalizer.IsFrench(_settings.Language) ? french : english;

    private string LocalizeRiskLevel(string riskLevel) => riskLevel.ToUpperInvariant() switch
    {
        "GREEN" => "VERT", "YELLOW" => "JAUNE", "RED" => "ROUGE", _ => riskLevel.ToUpperInvariant()
    };

    private string LocalizeUpdateMessage(string message)
    {
        if (!UiLocalizer.IsFrench(_settings.Language)) return message;
        return message switch
        {
            "Downloading release package…" => "Téléchargement du package de mise à jour…",
            "Verifying SHA-256…" => "Vérification SHA-256…",
            "Preparing update…" => "Préparation de la mise à jour…",
            "No public GitHub release is available yet, or the repository is not public." => "Aucune publication GitHub publique n’est encore disponible, ou le dépôt n’est pas public.",
            "The GitHub release does not provide a SHA-256 digest. Installation was cancelled." => "La publication GitHub ne fournit aucune empreinte SHA-256. L’installation a été annulée.",
            "The downloaded archive failed SHA-256 verification. Installation was cancelled." => "L’archive téléchargée a échoué à la vérification SHA-256. L’installation a été annulée.",
            "The release archive does not contain DSMS Preset Studio." => "L’archive de la publication ne contient pas DSMS Preset Studio.",
            "The running executable path is unavailable." => "Le chemin de l’exécutable en cours d’utilisation est indisponible.",
            "The update helper could not be started." => "L’assistant de mise à jour n’a pas pu démarrer.",
            _ => message
        };
    }

    private string LocalizeThemeError(string message)
    {
        if (!UiLocalizer.IsFrench(_settings.Language)) return message;
        if (message.StartsWith("'", StringComparison.Ordinal) &&
            message.EndsWith("is reserved for an Official Theme System. Rename the custom theme before importing it.", StringComparison.Ordinal))
            return message.Replace(
                "is reserved for an Official Theme System. Rename the custom theme before importing it.",
                "est réservé à un Official Theme System. Renommez le thème personnalisé avant de l’importer.",
                StringComparison.Ordinal);
        return message switch
        {
            "Theme files are limited to 20 MB. Compress the background image before exporting again." => "Les fichiers de thème sont limités à 20 Mo. Compressez l’image d’arrière-plan avant de réexporter le thème.",
            "The theme file is empty." => "Le fichier de thème est vide.",
            "The custom theme name is required." => "Le nom du thème personnalisé est obligatoire.",
            _ => message
        };
    }

    private void OpenGithub_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://github.com/Hoverbike51/DSMS-ModLoader") { UseShellExecute = true }); }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, Localized("Could not open the browser", "Impossible d’ouvrir le navigateur"), MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void OpenPatreon_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://www.patreon.com/Hoverbike/membership") { UseShellExecute = true }); }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, Localized("Could not open the browser", "Impossible d’ouvrir le navigateur"), MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void ShowJsonError(JsonException exception)
    {
        ValidateEditor();
        MessageBox.Show(this, exception.Message, Localized("Invalid JSON", "JSON invalide"), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void SetRisk(int errors, int warnings)
    {
        var french = UiLocalizer.IsFrench(_settings.Language);
        if (errors > 0)
        {
            RiskBadge.Background = (Brush)FindResource("ErrorBrush");
            RiskText.Text = french ? "ERREUR" : "ERROR";
            BottomRiskText.Text = french ? "✕ ERREUR" : "✕ ERROR";
            BottomRiskText.Foreground = (Brush)FindResource("ErrorBrush");
        }
        else if (warnings > 0)
        {
            RiskBadge.Background = (Brush)FindResource("WarningBrush");
            RiskText.Text = french ? "AVERTISSEMENT" : "WARNING";
            BottomRiskText.Text = french ? "! AVERTISSEMENT" : "! WARNING";
            BottomRiskText.Foreground = (Brush)FindResource("WarningBrush");
        }
        else
        {
            RiskBadge.Background = (Brush)FindResource("SuccessBrush");
            RiskText.Text = french ? "VALIDE" : "VALID";
            BottomRiskText.Text = french ? "✓ VALIDE" : "✓ VALID";
            BottomRiskText.Foreground = (Brush)FindResource("SuccessBrush");
        }
    }

    private void JsonEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (JsonEditor.IsInternalUpdate) return;
        if (!IsLoaded) return;
        _editorDirty = true;
        StatusText.Text = UiLocalizer.IsFrench(_settings.Language)
            ? "Modifications non enregistrées · utilisez Valider et corriger lorsque le document est prêt."
            : "Unsaved changes · press Validate & repair when ready.";
    }

    private string LocalizeIconSource(string source)
    {
        if (!UiLocalizer.IsFrench(_settings.Language)) return source;
        return source switch { "Generic" => "Générique", "Icons" => "Dossier Icons", _ => source };
    }

    private string LocalizeIconMessage(string message)
    {
        if (!UiLocalizer.IsFrench(_settings.Language)) return message;
        if (message.StartsWith("Local icon:", StringComparison.Ordinal)) return message.Replace("Local icon:", "Icône locale :");
        return message switch
        {
            "IconPath is empty; a generic icon is displayed." => "IconPath est vide ; une icône générique est affichée.",
            "IconPath syntax is invalid. Use /Game/Folder/Asset.Asset." => "La syntaxe d’IconPath est invalide. Utilisez /Game/Dossier/Asset.Asset.",
            "The requested icon was found, but another exported icon better matches this preset." => "L’icône demandée existe, mais une autre icône exportée semble mieux correspondre à ce preset.",
            "Icon resolved from the configured FModel export folder." => "Icône trouvée dans le dossier d’export FModel configuré.",
            "The requested asset was not exported, but a likely matching icon was found." => "L’asset demandé n’a pas été exporté, mais une icône correspondante probable a été trouvée.",
            "IconPath could not be resolved; a generic icon is displayed." => "IconPath est introuvable ; une icône générique est affichée.",
            _ => message
        };
    }

    private string LocalizeModLoaderState(string state)
    {
        if (!UiLocalizer.IsFrench(_settings.Language)) return state;
        return state switch
        {
            "UNKNOWN" => "INCONNU", "TOO OLD" => "TROP ANCIEN", "COMPATIBLE" => "COMPATIBLE",
            "NEWER" => "PLUS RÉCENT", "NOT FOUND" => "INTROUVABLE", _ => state
        };
    }

    private string LocalizeModLoaderMessage(ModLoaderStatus status)
    {
        if (!UiLocalizer.IsFrench(_settings.Language)) return status.Message;
        return status.State switch
        {
            "NOT FOUND" => "DSMS ModLoader est introuvable. Sélectionnez son dossier Scripts dans les paramètres.",
            "TOO OLD" => $"Version v{status.Version} installée ; la version compatible minimale est v{_compatibility.MinimumModLoaderVersion}.",
            "COMPATIBLE" => $"Version v{status.Version} installée et compatible avec JSON v3.",
            "NEWER" => $"Version v{status.Version} installée ; elle est plus récente que les versions testées avec ce Studio.",
            _ => status.Message
        };
    }

    private sealed record IssueRow(ValidationSeverity Severity, string Code, string Location, string Message)
    {
        public string SeverityLabel => UiLocalizer.Severity(Severity);
    }
}
