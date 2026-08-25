using System.Windows;
using System.Windows.Input;
using DSMS.PresetStudio.Localization;
using DSMS.PresetStudio.Services;

namespace DSMS.PresetStudio;

public partial class UpdatePromptWindow : Window
{
    public UpdatePromptWindow(StudioUpdate update, string language)
    {
        InitializeComponent();
        var french = UiLocalizer.IsFrench(language);
        Title = french ? "Mise à jour de DSMS Preset Studio" : "DSMS Preset Studio update";
        PromptTitleText.Text = french ? "Mise à jour disponible" : "Update available";
        PromptDescriptionText.Text = french
            ? "Une nouvelle version de DSMS Preset Studio est disponible. Souhaitez-vous la télécharger et l’installer maintenant ?"
            : "A new version of DSMS Preset Studio is available. Would you like to download and install it now?";
        CurrentLabelText.Text = french ? "INSTALLÉE" : "INSTALLED";
        AvailableLabelText.Text = french ? "DISPONIBLE" : "AVAILABLE";
        CurrentVersionText.Text = $"v{AppVersion.Current}";
        AvailableVersionText.Text = $"v{update.Version}";
        SecurityText.Text = french
            ? "Le package sera vérifié avec son empreinte SHA-256 avant l’installation. Vos paramètres et icônes importées seront conservés."
            : "The package will be verified with its SHA-256 digest before installation. Your settings and imported icons will be preserved.";
        LaterButton.Content = french ? "Plus tard" : "Later";
        UpdateButton.Content = french ? "Mettre à jour" : "Update now";
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Later_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Update_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
