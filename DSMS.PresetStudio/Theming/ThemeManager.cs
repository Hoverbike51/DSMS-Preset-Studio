using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DSMS.PresetStudio.Theming;

public static class ThemeManager
{
    public static IReadOnlyList<ThemeDefinition> BuiltInThemes { get; } =
    [
        new(),
        new ThemeDefinition
        {
            Name = "Dragon Violet", Background = "#0B0814", Panel = "#171126", PanelAlt = "#211936",
            Input = "#110D1D", Border = "#493766", Primary = "#B38CFF", Secondary = "#FF6FAE",
            TextPrimary = "#FAF7FF", TextSecondary = "#C4B7D8", Success = "#4AD99A", Warning = "#FFC857", Error = "#FF6B7A"
        },
        new ThemeDefinition
        {
            Name = "Steel Amber", Background = "#0D1013", Panel = "#171C21", PanelAlt = "#202830",
            Input = "#10161B", Border = "#3E4E5C", Primary = "#F5B942", Secondary = "#6CB6FF",
            TextPrimary = "#F7F8FA", TextSecondary = "#B7C0C8", Success = "#48D597", Warning = "#F5B942", Error = "#FF6678"
        }
    ];

    public static ThemeDefinition LoadFromFile(string filePath)
    {
        if (new FileInfo(filePath).Length > 20 * 1024 * 1024)
            throw new InvalidDataException("Theme files are limited to 20 MB. Compress the background image before exporting again.");
        return JsonSerializer.Deserialize<ThemeDefinition>(File.ReadAllText(filePath), JsonOptions)
               ?? throw new InvalidDataException("The theme file is empty.");
    }

    public static void Apply(ThemeDefinition theme)
    {
        var resources = Application.Current.Resources;
        SetBrush(resources, "AppBackgroundBrush", theme.Background);
        SetBrush(resources, "PanelBrush", theme.Panel);
        SetBrush(resources, "PanelAltBrush", theme.PanelAlt);
        SetBrush(resources, "InputBrush", theme.Input);
        SetBrush(resources, "BorderBrush", theme.Border);
        SetBrush(resources, "PrimaryBrush", theme.Primary);
        SetBrush(resources, "SecondaryBrush", theme.Secondary);
        SetBrush(resources, "TextPrimaryBrush", theme.TextPrimary);
        SetBrush(resources, "TextSecondaryBrush", theme.TextSecondary);
        SetBrush(resources, "SuccessBrush", theme.Success);
        SetBrush(resources, "WarningBrush", theme.Warning);
        SetBrush(resources, "ErrorBrush", theme.Error);
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

    private static void SetBrush(ResourceDictionary resources, string key, string colorText)
    {
        var color = (Color)ColorConverter.ConvertFromString(colorText);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        resources[key] = brush;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };
}
