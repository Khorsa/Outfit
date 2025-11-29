using OutfitTool.Services;
using OutfitTool.Services.Settings;
using System.Windows;
using System.Windows.Controls;
using OutfitTool.ModulesAdapter;

namespace OutfitTool.View;

/// <summary>
/// Логика взаимодействия для TabCommonSettings.xaml
/// </summary>
public partial class TabCommonSettings : UserControl
{
    public TabCommonSettings()
    {
        InitializeComponent();
    }

    private void Input_SettingsChanged(object sender, RoutedEventArgs e)
    {
        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

        var settings = settingsManager.LoadSettings();
        settings.MinimizeOnClose = minimizeOnClose.IsChecked ?? false;
        settings.MinimizeOnStart = minimizeOnStart.IsChecked ?? false;
        settings.LoadOnSystemStart = loadOnSystemStart.IsChecked ?? false;
        settings.CheckUpdatesOnStart = checkUpdatesOnStart.IsChecked ?? false;
        settings.InstallUpdates = installUpdates.IsChecked ?? false;
        settingsManager.SaveSettings(settings);
        ApplySettings();
    }

    private void UpdateSettingsCheckBoxes()
    {
        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

        var settings = settingsManager.LoadSettings();
        minimizeOnClose.IsChecked = settings.MinimizeOnClose;
        minimizeOnStart.IsChecked = settings.MinimizeOnStart;
        loadOnSystemStart.IsChecked = settings.LoadOnSystemStart;
        checkUpdatesOnStart.IsChecked = settings.CheckUpdatesOnStart;
        installUpdates.IsChecked = settings.InstallUpdates;
        ApplySettings();
    }

    private void ApplySettings()
    {
        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
        var startupManager = ServiceLocator.GetService<StartupManager>();

        var settings = settingsManager.LoadSettings();
        if (settings.LoadOnSystemStart)
        {
            startupManager.AddToStartup();
        }
        else
        {
            startupManager.RemoveFromStartup();
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateSettingsCheckBoxes();
    }
}