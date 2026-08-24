using DSMS.PresetStudio.Theming;

namespace DSMS.PresetStudio.Settings;

public sealed class AppSettings
{
    public string ThemeName { get; set; } = "Midnight Cyan";
    public string Language { get; set; } = "en-GB";
    public ThemeDefinition? CustomTheme { get; set; }
    public string FModelExportRoot { get; set; } = "";
    public string ModLoaderScriptsPath { get; set; } = "";
    public bool CheckForUpdatesOnStartup { get; set; }
}
