using System.Configuration;
using System.Data;
using System.Windows;
using DSMS.PresetStudio.Services;

namespace DSMS.PresetStudio;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.FirstOrDefault() == "--apply-update")
        {
            Shutdown(SelfUpdater.Run(e.Args));
            return;
        }
        base.OnStartup(e);
    }
}

