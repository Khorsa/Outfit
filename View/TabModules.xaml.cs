using OutfitTool.ModuleManager;
using OutfitTool.Services;
using OutfitTool.Services.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
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
using Module = OutfitTool.ModuleManager.Module;

namespace OutfitTool.View
{
    /// <summary>
    /// Логика взаимодействия для TabModules.xaml
    /// </summary>
    public partial class TabModules : UserControl
    {
        public TabModules()
        {
            InitializeComponent();
        }

        private void moduleList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (moduleList.SelectedItem is ListViewModuleItem selectedModule)
            {
                setButtons(selectedModule.module);
            }
            else
            {
                switchModule.IsEnabled = false;
                moduleSettings.IsEnabled = false;
            }
        }

        private void setButtons(Module module)
        {
            switchModule.IsEnabled = true;
            moduleSettings.IsEnabled = true;

            if (module.enabled)
            {
                switchModule.Content = LocalizationHelper.getString("Disable");
            }
            else
            {
                switchModule.Content = LocalizationHelper.getString("Enable");
            }
        }

        private void switchModule_Click_1(object sender, RoutedEventArgs e)
        {
            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

            if (moduleList.SelectedItem is ListViewModuleItem selectedModule)
            {
                selectedModule.module.enabled = !selectedModule.module.enabled;

                var settings = settingsManager.LoadSettings();

                if (selectedModule.module.enabled)
                {
                    settings.enabledModules.Add(selectedModule.module.assemblyName);
                }
                else
                {
                    settings.enabledModules.Remove(selectedModule.module.assemblyName);
                }
                settingsManager.SaveSettings(settings);

                initModules();
                initInterface();
            }
        }

        private void initModules()
        {
            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
            var moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();

            // Инициализация модулей
            var settings = settingsManager.LoadSettings();
            moduleManager.LoadModules(settings.enabledModules);
            foreach (Module module in moduleManager.GetModules())
            {
                if (module.enabled)
                {
                    module.moduleController.init();
                }
            }
        }

        private void initInterface()
        {
            var moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();
            var modules = moduleManager.GetModules();

            // Инициализация полного списка модулей
            var lvModuleItems = new ObservableCollection<ListViewModuleItem>();
            foreach (Module module in modules)
            {
                lvModuleItems.Add(new ListViewModuleItem(module));
            }
            ListViewModuleItem? selectedModule = null;
            if (moduleList.SelectedItem is ListViewModuleItem s)
            {
                selectedModule = s;
            }
            moduleList.ItemsSource = lvModuleItems;
            if (selectedModule != null)
            {
                foreach (ListViewModuleItem m in moduleList.Items)
                {
                    if (m.name == selectedModule.name)
                    {
                        moduleList.SelectedItem = m;
                        break;
                    }
                }
            }
        }


        private void moduleSettings_Click(object sender, RoutedEventArgs e)
        {
            if (moduleList.SelectedItem is ListViewModuleItem selectedModule)
            {
                selectedModule.module.moduleController.openSettings();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            initInterface();
        }
    }
}
