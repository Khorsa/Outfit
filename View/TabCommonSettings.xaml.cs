using OutfitTool.Services;
using OutfitTool.Services.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OutfitTool.View
{
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
            settings.minimizeOnClose = minimizeOnClose.IsChecked ?? false;
            settings.loadOnSystemStart = loadOnSystemStart.IsChecked ?? false;
            settings.checkUpdatesOnStart = checkUpdatesOnStart.IsChecked ?? false;
            settings.installUpdates = installUpdates.IsChecked ?? false;
            settings.installOnlyMinorUpdates = installOnlyMinorUpdates.IsChecked ?? false;
            settingsManager.SaveSettings(settings);
            applySettings();
        }

        private void updateSettingsCheckBoxes()
        {
            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

            var settings = settingsManager.LoadSettings();
            minimizeOnClose.IsChecked = settings.minimizeOnClose;
            loadOnSystemStart.IsChecked = settings.loadOnSystemStart;
            checkUpdatesOnStart.IsChecked = settings.checkUpdatesOnStart;
            installUpdates.IsChecked = settings.installUpdates;
            installOnlyMinorUpdates.IsChecked = settings.installOnlyMinorUpdates;
            applySettings();
        }

        private void applySettings()
        {
            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
            var startupManager = ServiceLocator.GetService<StartupManager>();

            var settings = settingsManager.LoadSettings();
            if (settings.loadOnSystemStart)
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
            ServiceLocator.RegisterServices();
            updateSettingsCheckBoxes();
        }
    }
}
