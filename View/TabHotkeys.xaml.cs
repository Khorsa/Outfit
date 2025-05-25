using OutfitTool.Common;
using OutfitTool.ModuleManager;
using OutfitTool.Services;
using OutfitTool.Services.HotkeyManager;
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
    /// Логика взаимодействия для TabHotkeys.xaml
    /// </summary>
    public partial class TabHotkeys : UserControl
    {
        public TabHotkeys()
        {
            InitializeComponent();
        }

    
        private void resetHotkey_Click(object sender, RoutedEventArgs e)
        {
            var hotKeyManager = ServiceLocator.GetService<HotKeyManager>();

            ListViewCommandItem? lvi = this.moduleCommandList.SelectedItem as ListViewCommandItem;
            if (lvi != null)
            {
                CommandDescriptor descriptor = new CommandDescriptor(lvi.module, lvi.command);
                hotKeyManager.clearKey(descriptor);
                lvi.hotKey = "";
                var list = moduleCommandList.ItemsSource;
                moduleCommandList.ItemsSource = null;
                moduleCommandList.ItemsSource = list;

                this.moduleCommandList.SelectedItem = lvi;
            }
        }

        private void setHotkey_Click(object sender, RoutedEventArgs e)
        {
            var hotKeyManager = ServiceLocator.GetService<HotKeyManager>();

            ListViewCommandItem? lvi = this.moduleCommandList.SelectedItem as ListViewCommandItem;
            if (lvi != null)
            {
                // Открываем окошко выбора клавиши
                SetHotKey setHotKey = new SetHotKey(lvi.module.moduleInfo.Name, lvi.command.Name, lvi.command.Description);
                if (setHotKey.ShowDialog() == true)
                {
                    HotKey key = setHotKey.getPressedKey();

                    CommandDescriptor descriptor = new CommandDescriptor(lvi.module, lvi.command);

                    hotKeyManager.registerKey(descriptor, key);

                    lvi.hotKey = key.ToString();

                    var list = moduleCommandList.ItemsSource;
                    moduleCommandList.ItemsSource = null;
                    moduleCommandList.ItemsSource = list;

                    this.moduleCommandList.SelectedItem = lvi;

                }
            }
        }


        private void initInterface()
        {
            var hotKeyManager = ServiceLocator.GetService<HotKeyManager>();
            var moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();

            var modules = moduleManager.GetModules();

            // Инициализация списка команд и горячих клавиш
            // В списке должны быть все команды всех включенных модулей и горячие клавиши из настроек
            List<ListViewCommandItem> list = new List<ListViewCommandItem>();
            foreach (Module module in modules)
            {
                if (module.enabled)
                {
                    foreach (CommandInterface command in module.moduleController.getCommandList())
                    {
                        HotKey? key = hotKeyManager.getKey(new CommandDescriptor(module, command));
                        list.Add(new ListViewCommandItem(command, module, key != null ? key.ToString() : ""));
                    }
                }
            }
            moduleCommandList.ItemsSource = list;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(moduleCommandList.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("module.moduleInfo.Name");
            view.GroupDescriptions.Add(groupDescription);
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            initInterface();
        }
    }
}
