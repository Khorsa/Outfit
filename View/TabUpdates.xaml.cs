using OutfitTool.Services;
using OutfitTool.Services.Settings;
using OutfitTool.Services.Updates;
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
using YamlDotNet.Serialization;

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
                var checkService = ServiceLocator.GetService<RepositoryItemConverter>();
                var ConvertedCollection = checkService.ConvertCollection(repositoryItems);
                lvRepositoryItemsList.ItemsSource = ConvertedCollection;
                foreach(var item in lvRepositoryItemsList.Items)
                {
                    if (
                        item is LastCompatibleRepositoryItem 
                        && StartParameters.ContainsKey("selected_item")
                        && (item as LastCompatibleRepositoryItem).Name == StartParameters.GetOne<string>("selected_item"))
                    {
                        lvRepositoryItemsList.SelectedItem = item;
                    }
                }
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

        private bool autoReadListDone = false;
        private void updatesRepository_Loaded(object sender, RoutedEventArgs e)
        {
            updateRepositorySettingsControl();

            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
            var settings = settingsManager.LoadSettings();
            var updatesManager = ServiceLocator.GetService<UpdatesManagerInterface>();
            if (!autoReadListDone && settings.checkUpdatesOnStart)
            {
                updatesManager.LoadList(updateManagerStatusChanged);
                autoReadListDone = true;
            }
            if (settings.installUpdates)
            {
                updatesManager.UpdateAll();
            }
        }

        private void lvRepositoryItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvRepositoryItemsList.SelectedItem is LastCompatibleRepositoryItem)
            {
                panelModuleDescription.Visibility = Visibility.Visible;
                panelModuleDescription.DataContext = lvRepositoryItemsList.SelectedItem;
            } else
            {
                panelModuleDescription.Visibility = Visibility.Hidden;
            }
        }

        private void updateRepositoryList_Click(object sender, RoutedEventArgs e)
        {
            var updatesManager = ServiceLocator.GetService<UpdatesManagerInterface>();
            updatesManager.LoadList(updateManagerStatusChanged);
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            if (lvRepositoryItemsList.SelectedItem is LastCompatibleRepositoryItem)
            {
                var item = (lvRepositoryItemsList.SelectedItem as LastCompatibleRepositoryItem);
                item.CanUpdate = false;
                item.Status = LastCompatibleRepositoryItemStatus.Downloading;

                var updatePlate = new UpdateWindow(item);
                updatePlate.ShowDialog("update");

                panelModuleDescription.DataContext = null;
                panelModuleDescription.DataContext = item;
            }
        }

        private void Install_Click(object sender, RoutedEventArgs e)
        {
            if (lvRepositoryItemsList.SelectedItem is LastCompatibleRepositoryItem)
            {
                var item = (lvRepositoryItemsList.SelectedItem as LastCompatibleRepositoryItem);
                item.CanInstall = false;
                item.Status = LastCompatibleRepositoryItemStatus.Downloading;

                var updatePlate = new UpdateWindow(item);
                updatePlate.ShowDialog("install");

                panelModuleDescription.DataContext = null;
                panelModuleDescription.DataContext = item;
            }
        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (lvRepositoryItemsList.SelectedItem is LastCompatibleRepositoryItem)
            {
                var item = (lvRepositoryItemsList.SelectedItem as LastCompatibleRepositoryItem);
                item.CanDelete = false;
                item.Status = LastCompatibleRepositoryItemStatus.NotAvailable;

                var updatePlate = new UpdateWindow(item);
                updatePlate.ShowDialog("delete");

                panelModuleDescription.DataContext = null;
                panelModuleDescription.DataContext = item;
            }
        }
    }
}
