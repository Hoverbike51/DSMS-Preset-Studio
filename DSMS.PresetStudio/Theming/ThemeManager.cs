using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DSMS.PresetStudio.Theming;

public static class ThemeManager
{
    public const string OfficialSystemAuthor = "HoverModsVault";

    public static IReadOnlyList<ThemeDefinition> BuiltInThemes { get; } =
    [
        Official(new ThemeDefinition { Name = "Midnight Cyan" }),
        Official(new ThemeDefinition
        {
            Name = "Dragon Violet", Background = "#0B0814", Panel = "#171126", PanelAlt = "#211936",
            Input = "#110D1D", Border = "#493766", Primary = "#B38CFF", Secondary = "#FF6FAE",
            TextPrimary = "#FAF7FF", TextSecondary = "#C4B7D8", Success = "#4AD99A", Warning = "#FFC857", Error = "#FF6B7A"
        }),
        Official(new ThemeDefinition
        {
            Name = "Steel Amber", Background = "#0D1013", Panel = "#171C21", PanelAlt = "#202830",
            Input = "#10161B", Border = "#3E4E5C", Primary = "#F5B942", Secondary = "#6CB6FF",
            TextPrimary = "#F7F8FA", TextSecondary = "#B7C0C8", Success = "#48D597", Warning = "#F5B942", Error = "#FF6678"
        })
    ];

    public static ThemeDefinition LoadFromFile(string filePath)
    {
        if (new FileInfo(filePath).Length > 20 * 1024 * 1024)
            throw new InvalidDataException("Theme files are limited to 20 MB. Compress the background image before exporting again.");
        var theme = JsonSerializer.Deserialize<ThemeDefinition>(File.ReadAllText(filePath), JsonOptions)
                    ?? throw new InvalidDataException("The theme file is empty.");
        theme.IsOfficialSystemTheme = false;
        theme.Name = theme.Name.Trim();
        theme.Author = string.IsNullOrWhiteSpace(theme.Author) ? "Unknown" : theme.Author.Trim();
        if (string.IsNullOrWhiteSpace(theme.Name)) throw new InvalidDataException("The custom theme name is required.");
        if (IsSystemThemeName(theme.Name))
            throw new InvalidDataException($"'{theme.Name}' is reserved for an Official Theme System. Rename the custom theme before importing it.");
        return theme;
    }

    public static bool IsSystemThemeName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && BuiltInThemes.Any(x => x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

    public static ThemeDefinition CreateCustomCopy(ThemeDefinition source)
    {
        var copy = new ThemeDefinition
        {
            Name = source.IsOfficialSystemTheme ? $"{source.Name} Custom" : source.Name,
            Author = source.IsOfficialSystemTheme ? "Unknown" : source.Author,
            Background = source.Background, Panel = source.Panel, PanelAlt = source.PanelAlt,
            Input = source.Input, Border = source.Border, Primary = source.Primary, Secondary = source.Secondary,
            TextPrimary = source.TextPrimary, TextSecondary = source.TextSecondary,
            Success = source.Success, Warning = source.Warning, Error = source.Error,
            FontFamily = source.FontFamily, FontSize = source.FontSize,
            UiOpacity = source.UiOpacity, TextOpacity = source.TextOpacity,
            BackgroundImageBase64 = source.BackgroundImageBase64,
            BackgroundImageOpacity = source.BackgroundImageOpacity
        };
        return copy;
    }

    public static string SerializeCustom(ThemeDefinition theme) =>
        JsonSerializer.Serialize(CreateCustomCopy(theme), JsonOptions);

    public static void Apply(ThemeDefinition theme)
    {
        var resources = Application.Current.Resources;
        var uiOpacity = Math.Clamp(theme.UiOpacity, 0, 1);
        var textOpacity = Math.Clamp(theme.TextOpacity, 0, 1);
        SetBrush(resources, "AppBackgroundBrush", theme.Background);
        SetBrush(resources, "PanelBrush", theme.Panel, uiOpacity);
        SetBrush(resources, "PanelAltBrush", theme.PanelAlt, uiOpacity);
        SetBrush(resources, "InputBrush", theme.Input, uiOpacity);
        SetBrush(resources, "BorderBrush", theme.Border, uiOpacity);
        SetBrush(resources, "PrimaryBrush", theme.Primary, uiOpacity);
        SetBrush(resources, "SecondaryBrush", theme.Secondary, uiOpacity);
        SetBrush(resources, "TextPrimaryBrush", theme.TextPrimary, textOpacity);
        SetBrush(resources, "TextSecondaryBrush", theme.TextSecondary, textOpacity);
        SetBrush(resources, "SuccessBrush", theme.Success, textOpacity);
        SetBrush(resources, "WarningBrush", theme.Warning, textOpacity);
        SetBrush(resources, "ErrorBrush", theme.Error, textOpacity);
        SetBrush(resources, "JsonPropertyBrush", theme.Primary, textOpacity);
        SetBrush(resources, "JsonStringBrush", theme.Success, textOpacity);
        SetBrush(resources, "JsonNumberBrush", theme.Warning, textOpacity);
        SetBrush(resources, "JsonKeywordBrush", theme.Secondary, textOpacity);
        SetBrush(resources, "JsonDefaultBrush", theme.TextPrimary, textOpacity);
        resources["AppFontFamily"] = new FontFamily(string.IsNullOrWhiteSpace(theme.FontFamily) ? "Segoe UI" : theme.FontFamily.Trim());
        resources["AppFontSize"] = Math.Clamp(theme.FontSize, 10, 18);
        resources["AppBackgroundImageBrush"] = CreateBackgroundBrush(theme);
    }

    private static Brush CreateBackgroundBrush(ThemeDefinition theme)
    {
        if (string.IsNullOrWhiteSpace(theme.BackgroundImageBase64)) return Brushes.Transparent;
        try
        {
            var value = theme.BackgroundImageBase64;
            var comma = value.IndexOf(',');
            if (comma >= 0) value = value[(comma + 1)..];
            var bytes = Convert.FromBase64String(value);
            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return new ImageBrush(image)
            {
                Stretch = Stretch.UniformToFill,
                Opacity = Math.Clamp(theme.BackgroundImageOpacity, 0, 0.5)
            };
        }
        catch
        {
            return Brushes.Transparent;
        }
    }

    private static void SetBrush(ResourceDictionary resources, string key, string colorText, double opacity = 1)
    {
        var color = (Color)ColorConverter.ConvertFromString(colorText);
        var brush = new SolidColorBrush(color) { Opacity = opacity };
        brush.Freeze();
        resources[key] = brush;
    }

    private static ThemeDefinition Official(ThemeDefinition theme)
    {
        theme.Author = OfficialSystemAuthor;
        theme.IsOfficialSystemTheme = true;
        return theme;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };
}
