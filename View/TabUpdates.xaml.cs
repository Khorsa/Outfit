using OutfitTool.Services;
using OutfitTool.Services.Settings;
using OutfitTool.Services.Updates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OutfitTool.ModulesAdapter;

namespace OutfitTool.View;

/// <summary>
/// Логика взаимодействия для TabUpdates.xaml
/// </summary>
public partial class TabUpdates
{
    public TabUpdates()
    {
        InitializeComponent();
    }

    private void UpdateRepositorySettingsControl()
    {
        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

        var settings = settingsManager.LoadSettings();
        updatesRepository.Text = settings.UpdatesRepository;
        defaultUpdatesRepository.Content = AppSettings.DEFAULT_UPDATES_REPOSITORY;
        ApplySettings();
    }

    private void UpdateManagerStatusChanged(bool busy, RepositoryCollection? repositoryItems)
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
            var convertedCollection = checkService.ConvertCollection(repositoryItems);
            lvRepositoryItemsList.ItemsSource = convertedCollection;
            foreach(var item in lvRepositoryItemsList.Items)
            {
                if (
                    item is LastCompatibleRepositoryItem 
                    && StartParameters.ContainsKey("selected_item")
                    && (item as LastCompatibleRepositoryItem)?.Name == StartParameters.GetOne<string>("selected_item"))
                {
                    lvRepositoryItemsList.SelectedItem = item;
                }
            }
        }
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

    private void DefaultRepository_MouseUp(object sender, MouseButtonEventArgs e)
    {
        updatesRepository.Text = AppSettings.DEFAULT_UPDATES_REPOSITORY;
    }


    private void UpdateRepository_SettingsChanged(object sender, RoutedEventArgs e)
    {
        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
        var settings = settingsManager.LoadSettings();
        settings.UpdatesRepository = updatesRepository.Text;
        settingsManager.SaveSettings(settings);
        ApplySettings();
    }

    private bool _autoReadListDone;
    private void updatesRepository_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateRepositorySettingsControl();

        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
        var settings = settingsManager.LoadSettings();
        var updatesManager = ServiceLocator.GetService<IUpdatesManagerInterface>();
        if (!_autoReadListDone && settings.CheckUpdatesOnStart)
        {
            updatesManager.LoadList(UpdateManagerStatusChanged);
            _autoReadListDone = true;
        }
        if (settings.InstallUpdates)
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
        var updatesManager = ServiceLocator.GetService<IUpdatesManagerInterface>();
        updatesManager.LoadList(UpdateManagerStatusChanged);
    }

    private void Update_Click(object sender, RoutedEventArgs e)
    {
        if (lvRepositoryItemsList.SelectedItem is not LastCompatibleRepositoryItem) return;
        var item = (lvRepositoryItemsList.SelectedItem as LastCompatibleRepositoryItem);
        if (item == null) return;
            
        item.CanUpdate = false;
        item.Status = LastCompatibleRepositoryItemStatus.Downloading;

        var updatePlate = new UpdateWindow(item);
        updatePlate.ShowDialog("update");

        panelModuleDescription.DataContext = null;
        panelModuleDescription.DataContext = item;
    }

    private void Install_Click(object sender, RoutedEventArgs e)
    {
        if (lvRepositoryItemsList.SelectedItem is not LastCompatibleRepositoryItem) return;
        
        var item = (lvRepositoryItemsList.SelectedItem as LastCompatibleRepositoryItem);
        if (item == null) return;
        item.CanInstall = false;
        item.Status = LastCompatibleRepositoryItemStatus.Downloading;

        var updatePlate = new UpdateWindow(item);
        updatePlate.ShowDialog("install");

        panelModuleDescription.DataContext = null;
        panelModuleDescription.DataContext = item;
    }
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (lvRepositoryItemsList.SelectedItem is not LastCompatibleRepositoryItem) return;
        
        var item = (lvRepositoryItemsList.SelectedItem as LastCompatibleRepositoryItem);
        if (item == null) return;
        
        item.CanDelete = false;
        item.Status = LastCompatibleRepositoryItemStatus.NotAvailable;
        var updatePlate = new UpdateWindow(item);
        updatePlate.ShowDialog("delete");
        panelModuleDescription.DataContext = null;
        panelModuleDescription.DataContext = item;
    }
}