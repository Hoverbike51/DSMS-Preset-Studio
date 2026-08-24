namespace DSMS.PresetStudio.Theming;

public sealed class ThemeDefinition
{
    public string Name { get; set; } = "Midnight Cyan";
    public string Author { get; set; } = "HoverMods Vault";
    public string Background { get; set; } = "#080D16";
    public string Panel { get; set; } = "#101927";
    public string PanelAlt { get; set; } = "#162235";
    public string Input { get; set; } = "#0C1522";
    public string Border { get; set; } = "#2A405A";
    public string Primary { get; set; } = "#23CFE5";
    public string Secondary { get; set; } = "#8B7CFF";
    public string TextPrimary { get; set; } = "#F4F7FB";
    public string TextSecondary { get; set; } = "#A8B5C7";
    public string Success { get; set; } = "#35D07F";
    public string Warning { get; set; } = "#F5B942";
    public string Error { get; set; } = "#FF6174";
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 13;
    public string? BackgroundImageBase64 { get; set; }
    public double BackgroundImageOpacity { get; set; } = 0.12;

    public override string ToString() => Name;
}
