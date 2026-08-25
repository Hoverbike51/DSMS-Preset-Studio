using System.IO;
using System.Text.Json;

namespace DSMS.PresetStudio.Settings;

public static class SettingsService
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HoverModsVault", "DSMSPresetStudio");
    private static readonly string FilePath = Path.Combine(DirectoryPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppSettings();
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options) ?? new AppSettings();
            settings.CustomThemes ??= [];
            if (settings.SettingsSchemaVersion < 2)
            {
                // The setting existed internally before it was exposed in the UI and was always false.
                // Enable the new startup check once; subsequent user choices are preserved by schema v2.
                settings.CheckForUpdatesOnStartup = true;
                settings.SettingsSchemaVersion = 2;
                Save(settings);
            }
            if (settings.SettingsSchemaVersion < 3)
            {
                if (settings.CustomTheme is not null && settings.CustomThemes.All(x =>
                        !x.Name.Equals(settings.CustomTheme.Name, StringComparison.OrdinalIgnoreCase)))
                    settings.CustomThemes.Add(settings.CustomTheme);
                settings.CustomTheme = null;
                settings.SettingsSchemaVersion = 3;
                Save(settings);
            }
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));
        }
        catch
        {
            // Settings must never prevent the editor or validator from running.
        }
    }

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
}
