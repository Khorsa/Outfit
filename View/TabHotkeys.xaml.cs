using OutfitTool.ModulesAdapter.ModuleManager;
using OutfitTool.Services;
using OutfitTool.Services.HotkeyManager;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OutfitTool.ModulesAdapter;

namespace OutfitTool.View;

/// <summary>
/// Логика взаимодействия для TabHotkeys.xaml
/// </summary>
public partial class TabHotkeys
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
            CommandDescriptor descriptor = new CommandDescriptor(lvi.Module, lvi.Command);
            hotKeyManager.ClearKey(descriptor);
            lvi.HotKey = "";
            var list = moduleCommandList.ItemsSource;
            moduleCommandList.ItemsSource = null;
            moduleCommandList.ItemsSource = list;

            this.moduleCommandList.SelectedItem = lvi;
        }
    }

    private void setHotkey_Click(object sender, RoutedEventArgs e)
    {
        var hotKeyManager = ServiceLocator.GetService<HotKeyManager>();

        var lvi = moduleCommandList.SelectedItem as ListViewCommandItem;
        if (lvi == null) return;
        // Открываем окошко выбора клавиши
        var setHotKey = new SetHotKey(lvi.Module.ModuleInfo.Name, lvi.Command.Name, lvi.Command.Description);
        if (setHotKey.ShowDialog() != true) return;
        var key = setHotKey.GetPressedKey();
        var descriptor = new CommandDescriptor(lvi.Module, lvi.Command);
        if (key != null)
        {
            hotKeyManager.RegisterKey(descriptor, key);
            lvi.HotKey = key.ToString();
        }

        var list = moduleCommandList.ItemsSource;
        moduleCommandList.ItemsSource = null;
        moduleCommandList.ItemsSource = list;
        moduleCommandList.SelectedItem = lvi;
    }


    private void InitInterface()
    {
        var hotKeyManager = ServiceLocator.GetService<HotKeyManager>();
        var moduleManager = ServiceLocator.GetService<IModuleManager>();

        var modules = moduleManager.GetModules();

        // Инициализация списка команд и горячих клавиш
        // В списке должны быть все команды всех включенных модулей и горячие клавиши из настроек
        List<ListViewCommandItem> list = new List<ListViewCommandItem>();
        foreach (Module module in modules)
        {
            if (module.Enabled)
            {
                foreach (ICommand command in module.ModuleController.GetCommandList())
                {
                    HotKey? key = hotKeyManager.GetKey(new CommandDescriptor(module, command));
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
        InitInterface();
    }
}