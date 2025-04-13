using OutfitTool.Services.Settings;
using OutfitTool.Services.Updates;
using OutfitTool.Services;
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
    /// Логика взаимодействия для TabUpdates.xaml
    /// </summary>
    public partial class TabUpdates : UserControl
    {
        public TabUpdates()
        {
            InitializeComponent();

            ServiceLocator.RegisterServices();
            var updatesManager = ServiceLocator.GetService<UpdatesManager>();
            updatesManager.updateManagerStatusChanged = updateManagerStatusChanged;
        }

        private void updateRepositorySettingsControl()
        {
            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

            var settings = settingsManager.LoadSettings();
            updatesRepository.Text = settings.updatesRepository;
            defaultUpdatesRepository.Content = settings.defaultUpdatesRepository;
            applySettings();
        }

        private void updateManagerStatusChanged(bool busy, RepositoryCollection? repositoryItems)
        {
            if (busy)
            {
                this.updatesManagerPlug.Visibility = Visibility.Visible;
                lvRepositoryItemsList.Visibility = Visibility.Hidden;
            }
            else
            {
                this.updatesManagerPlug.Visibility = Visibility.Hidden;
                lvRepositoryItemsList.Visibility = Visibility.Visible;
            }
            if (repositoryItems != null)
            {
                lvRepositoryItemsList.ItemsSource = repositoryItems.GetModulesNames();
            }
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

        private void DefaultRepository_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
            var settings = settingsManager.LoadSettings();
            updatesRepository.Text = settings.defaultUpdatesRepository;
        }


        private void UpdateRepository_SettingsChanged(object sender, RoutedEventArgs e)
        {
            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
            var settings = settingsManager.LoadSettings();
            settings.updatesRepository = updatesRepository.Text;
            settingsManager.SaveSettings(settings);
            applySettings();
        }

        private void updatesRepository_Loaded(object sender, RoutedEventArgs e)
        {
            ServiceLocator.RegisterServices();

            updateRepositorySettingsControl();
        }
    }
}
