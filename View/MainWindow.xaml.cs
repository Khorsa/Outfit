using Common.Logger;
using Microsoft.VisualBasic.ApplicationServices;
using OutfitTool.Common;
using OutfitTool.ModuleManager;
using OutfitTool.Services;
using OutfitTool.Services.HotkeyManager;
using OutfitTool.Services.Settings;
using OutfitTool.Services.Updates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YamlDotNet.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using Module = OutfitTool.ModuleManager.Module;
using System.Management;
using System.Timers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;
using System.Windows.Threading;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using Hardcodet.Wpf.TaskbarNotification;
using System.Collections.ObjectModel;
using OutfitTool.View;

namespace OutfitTool
{
    public partial class MainWindow : Window
    {
        public const int WM_HOTKEY = 0x0312; // 786
        public const int ICON_BLINK_INTERVAL = 400;

        private static readonly DispatcherTimer iconTimer = new();

        LoggerInterface logger;
        SettingsManager<AppSettings> settingsManager;
        StartupManager startupManager;
        UpdatesManager updatesManager;
        HotKeyManager hotKeyManager;
        ModuleManagerInterface moduleManager;

        public MainWindow()
        {
            InitializeComponent();
            ServiceLocator.RegisterServices();

            LocalizationHelper.LanguageChanged += LanguageChanged;

            settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
            startupManager = ServiceLocator.GetService<StartupManager>();
            updatesManager = ServiceLocator.GetService<UpdatesManager>();
            moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();
            hotKeyManager = ServiceLocator.GetService<HotKeyManager>();

            initLogger();
            initIconTimer();
        }

        private void initLogger()
        {
            logger = ServiceLocator.GetService<LoggerInterface>();
            logger.Subscribe(this.logHandler);
        }


        private void initIconTimer()
        {
            iconTimer.Interval = TimeSpan.FromMilliseconds(ICON_BLINK_INTERVAL);
            iconTimer.Tick += OnModulesTimedEvent;
            iconTimer.Start();
        }


        private void LanguageChanged(Object? sender, EventArgs e)
        {
            initInterface();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            initModules();
            hotKeyManager.init(new WindowInteropHelper(this).Handle);
            initInterface();
            updateSettingsCheckBoxes();

            var settings = settingsManager.LoadSettings();
            settingsManager.SaveSettings(settings);
            LocalizationHelper.Language = new CultureInfo(settings.language);

            var source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);

            if (settings.checkUpdatesOnStart == true && updatesManager != null)
            {
                updatesManager.LoadList(fillUpdatesList);

            }
        }

        private void fillUpdatesList(List<RepositoryItem> repositoryItems)
        {
            RepositoryStatus.Text = "";
            foreach (var updates in repositoryItems)
            {
                RepositoryStatus.Text += updates.ToString() + "\r\n";
            }
        }


        private void initModules() 
        {
            // Инициализация модулей
            var settings = settingsManager.LoadSettings();
            moduleManager.LoadModules(settings.enabledModules);
            foreach (Module module in moduleManager.getModules())
            {
                if (module.enabled)
                {
                    module.moduleController.init();
                }
            }
        }

        private void initInterface()
        {
            TaskbarIcon.Visibility = Visibility.Visible;

            var modules = moduleManager.getModules();
            var settings = settingsManager.LoadSettings();

            // Установка языка
            foreach (Module module in modules)
            {
                module.moduleController.setLanguage(settings.language);
            }

            // Инициализация контекстного меню
            TrayContextMenu.Items.Clear();
            foreach (Module module in modules)
            {
                if (module.enabled)
                {
                    foreach (CommandInterface command in module.moduleController.getCommandList())
                    {
                        CommandContextMenu? menu = command.ContextMenu;
                        if (menu != null)
                        {
                            TrayContextMenu.Items.Add(createTrayMenuItem(menu.text, new Image { Source = menu.image, Width = 16, Height = 16 }, ModuleCommandClick, command));
                        }
                    }
                    TrayContextMenu.Items.Add(new Separator());
                }
            }
            TrayContextMenu.Items.Add(getTrayMenuItem(LocalizationHelper.getString("Settings"), "gear.png", OpenSettings));
            MenuItem laguageItem = getTrayMenuItem(LocalizationHelper.getString("Language"), "font.png");
            foreach (var l in LocalizationHelper.languages)
            {
                laguageItem.Items.Add(getTrayMenuItem(l.Value.Name, l.Value.FlagResource, ChangeLanguage, l.Value.CultureName));
            }
            TrayContextMenu.Items.Add(laguageItem);
            TrayContextMenu.Items.Add(getTrayMenuItem(LocalizationHelper.getString("Exit"), "navigate_cross.png", Exit_Click));


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





        private MenuItem createTrayMenuItem(string title, Image? image, RoutedEventHandler? onClick = null, object? Tag = null)
        {
            MenuItem menuItem = new MenuItem();
            menuItem.Header = title;
            if (onClick != null)
            {
                menuItem.Click += onClick;
            }
            if (Tag != null)
            {
                menuItem.Tag = Tag;
            }
            if (image != null)
            {
                menuItem.Icon = image;
            }
            return menuItem;
        }

        private MenuItem getTrayMenuItem(string title, string? resource, RoutedEventHandler? onClick = null, object? tag = null)
        {
            Image? image = null;
            if (resource != null){
                BitmapImage icon = new BitmapImage(new Uri("pack://application:,,,/Resources/" + resource));
                image = new Image { Source = icon, Width = 16, Height = 16 };
            }
            return this.createTrayMenuItem(title, image, onClick, tag);
        }


        private void ChangeLanguage(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem clickedItem && clickedItem.Tag is string language)
            {
                var settings = settingsManager.LoadSettings();
                settings.language = language;
                settingsManager.SaveSettings(settings);
                LocalizationHelper.Language = new CultureInfo(language);
            }
        }


        private void ModuleCommandClick(object sender, RoutedEventArgs e)
        {
            MenuItem? clickedItem = sender as MenuItem;
            if (clickedItem != null && clickedItem.Tag != null && clickedItem.Tag is CommandInterface)
            {
                CommandInterface command = clickedItem.Tag as CommandInterface;
                command.run();
            }
        }

        private void logHandler(object? sender, LogEntry logEntry)
        {
            string message = logEntry.LogDateTime.ToString() + " " + logEntry.Level.ToString() + ": " + logEntry.Message;
            logsBlock.Text += message + "\r\n";
            Debug.WriteLine(message);

            if (logEntry.Level == LogLevel.status)
            {
                StatusBarItemInfo.Content = logEntry.Message;
            }
        }



        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            this.Show();
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            this.Activate();
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }


        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
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
            if (moduleList.SelectedItem is ListViewModuleItem selectedModule)
            {
                selectedModule.module.enabled = !selectedModule.module.enabled;

                var settings = settingsManager.LoadSettings();

                if (selectedModule.module.enabled){
                    settings.enabledModules.Add(selectedModule.module.assemblyName);
                } else
                {
                    settings.enabledModules.Remove(selectedModule.module.assemblyName);
                }
                settingsManager.SaveSettings(settings);
                
                initModules();
                initInterface();
            }
        }

        private void Input_SettingsChanged(object sender, RoutedEventArgs e)
        {
            var settings = settingsManager.LoadSettings();
            settings.minimizeOnClose = minimizeOnClose.IsChecked ?? false;
            settings.loadOnSystemStart = loadOnSystemStart.IsChecked ?? false;
            settings.checkUpdatesOnStart = checkUpdatesOnStart.IsChecked ?? false;
            settings.installUpdates = installUpdates.IsChecked ?? false;
            settings.installOnlyMinorUpdates = installOnlyMinorUpdates.IsChecked ?? false;
            settings.updatesRepository = updatesRepository.Text;
            settingsManager.SaveSettings(settings);
            applySettings();
        }

        private void updateSettingsCheckBoxes()
        {
            var settings = settingsManager.LoadSettings();
            minimizeOnClose.IsChecked = settings.minimizeOnClose;
            loadOnSystemStart.IsChecked = settings.loadOnSystemStart;
            checkUpdatesOnStart.IsChecked = settings.checkUpdatesOnStart;
            installUpdates.IsChecked = settings.installUpdates;
            installOnlyMinorUpdates.IsChecked = settings.installOnlyMinorUpdates;
            updatesRepository.Text = settings.updatesRepository;
            defaultUpdatesRepository.Content = settings.defaultUpdatesRepository;
            applySettings();
        }

        private void DefaultRepository_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var settings = settingsManager.LoadSettings();
            updatesRepository.Text = settings.defaultUpdatesRepository;    
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            var settings = settingsManager.LoadSettings();
            if (settings.minimizeOnClose)
            {
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
            }
        }

        private void applySettings()
        {
            var settings = settingsManager.LoadSettings();
            if (settings.loadOnSystemStart) {
                startupManager.AddToStartup();
            } else {
                startupManager.RemoveFromStartup();
            }
        }

        private void resetHotkey_Click(object sender, RoutedEventArgs e)
        {
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

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY) 
            {
                hotKeyManager.hotKeyPressed(wParam.ToInt32());
            }
            return IntPtr.Zero;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            var settings = settingsManager.LoadSettings();
            if (!settings.minimizeOnClose)
            {
                var source = PresentationSource.FromVisual(this) as HwndSource;
                source?.RemoveHook(WndProc);
                var modules = this.moduleManager.getModules();
                foreach (Module module in modules)
                {
                    if (module.enabled)
                    {
                        module.moduleController.shutdown();
                    }
                }

                hotKeyManager.clearAllKeys();
                Application.Current.Shutdown();
            }
        }

        private int currentIconIndex = -1;
        private void OnModulesTimedEvent(object? sender, EventArgs e)
        {
            TaskbarIcon.IconSource = this.getTaskbarIcon();
            TaskbarIcon.ToolTipText = this.getTaskbarIconText();
            checkNotifications();
        }

        private Notification? lastNotification = null;
        private void checkNotifications()
        {
            var modules = this.moduleManager.getModules();
            foreach (Module module in modules)
            {
                if (module.enabled)
                {
                    var notification = module.moduleController.popNotification();
                    if (notification != null)
                    {
                        lastNotification = notification;
                        TaskbarIcon.ShowBalloonTip(notification.title, notification.message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    }
                }
            }
        }

        private void clickNotification(object sender, EventArgs e)
        {
            if (lastNotification != null)
            {
                Process.Start(new ProcessStartInfo(lastNotification.href) { UseShellExecute = true });
            }
        }


        private ImageSource getTaskbarIcon()
        {
            ImageSource icon = new BitmapImage(new Uri("pack://application:,,,/Resources/wrench.ico"));
            if (currentIconIndex != -1)
            {
                var modules = moduleManager.getModules();
                var imageSources = new List<BitmapImage>();
                foreach (Module module in modules)
                {
                    if (module.enabled)
                    {
                        var i = module.moduleController.getTaskbarIcon();
                        if (i != null)
                        {
                            imageSources.Add(i);
                        }
                    }
                }

                if (currentIconIndex >= 0 && currentIconIndex < imageSources.Count())
                {
                    icon = imageSources[currentIconIndex];
                }

                currentIconIndex++;
                if (currentIconIndex >= imageSources.Count())
                {
                    currentIconIndex = -1;
                }
            }
            else
            {
                currentIconIndex = 0;
            }

            return icon;
        }

        private string getTaskbarIconText()
        {
            string text = "Outfit Tool";

            var modules = this.moduleManager.getModules();
            foreach (Module module in modules)
            {
                if (module.enabled)
                {
                    string? moduleTaskbarIconText = module.moduleController.getTaskbarIconText();
                    if (moduleTaskbarIconText != null) {
                        text = text + "\r\n" + module.moduleInfo.Name + ": " + moduleTaskbarIconText;
                    }
                }
            }

            return text;
        }

        private void moduleSettings_Click(object sender, RoutedEventArgs e)
        {
            if (moduleList.SelectedItem is ListViewModuleItem selectedModule)
            {
                selectedModule.module.moduleController.openSettings();
            }
        }
    }
}