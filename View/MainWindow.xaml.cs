using OutfitTool.Common;
using OutfitTool.ModuleManager;
using OutfitTool.Services;
using OutfitTool.Services.HotkeyManager;
using OutfitTool.Services.Settings;
using OutfitTool.Services.Updates;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Module = OutfitTool.ModuleManager.Module;
using Common.Logger;

namespace OutfitTool.View
{
    public partial class MainWindow : Window
    {
        public const int WM_HOTKEY = 0x0312; // 786
        public const int ICON_BLINK_INTERVAL = 400;
        private static readonly DispatcherTimer iconTimer = new();
        public static MainWindow Instance { get; private set; }

        public MainWindow()
        {
            MainWindow.Instance = this;
            InitializeComponent();

            LocalizationHelper.LanguageChanged += LanguageChanged;

            initIconTimer();
            initModules();

            ServiceLocator.GetService<LoggerInterface>().Subscribe(this.statusLogHandler);
        }

        private void statusLogHandler(object? sender, LogEntry logEntry)
        {
            if (logEntry.Level == LogLevel.status)
            {
                StatusBarItemInfo.Content = logEntry.Message;
            }
        }

        private void initIconTimer()
        {
            iconTimer.Interval = TimeSpan.FromMilliseconds(ICON_BLINK_INTERVAL);
            iconTimer.Tick += OnModulesTimedEvent;
            iconTimer.Start();
        }

        // Инициализация модулей
        private void initModules()
        {           
            var moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();
            var settings = ServiceLocator.GetService<SettingsManager<AppSettings>>().LoadSettings();

            moduleManager.LoadModules(settings.enabledModules);
            foreach (Module module in moduleManager.GetModules())
            {
                if (module.enabled)
                {
                    module.moduleController.init();
                }
            }
        }

        private void LanguageChanged(Object? sender, EventArgs e)
        {
            initInterface();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hotKeyManager = ServiceLocator.GetService<HotKeyManager>();
            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

            hotKeyManager.init(new WindowInteropHelper(this).Handle);
            initInterface();
            

            var settings = settingsManager.LoadSettings();
            settingsManager.SaveSettings(settings);
            LocalizationHelper.Language = new CultureInfo(settings.language);

            var source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);






            if (StartParameters.ContainsKey("selected_tab"))
            {
                TabControl.SelectedIndex = StartParameters.GetOne<int>("selected_tab");
            }

            // Устанавливаем переданные размеры и положение окна
            if (StartParameters.ContainsKey("left"))
            {
                this.Left = StartParameters.GetOne<int>("left");
            }
            if (StartParameters.ContainsKey("top"))
            {
                this.Top = StartParameters.GetOne<int>("top");
            }
            if (StartParameters.ContainsKey("width"))
            {
                this.Width = StartParameters.GetOne<int>("width");
            }
            if (StartParameters.ContainsKey("height"))
            {
                this.Height = StartParameters.GetOne<int>("height");
            }

        }



        private void initInterface()
        {
            var moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();
            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

            TaskbarIcon.Visibility = Visibility.Visible;

            var modules = moduleManager.GetModules();
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
                            TrayContextMenu.Items.Add(createTrayMenuItem(
                                menu.text, 
                                new Image { Source = menu.image, Width = 16, Height = 16 }, 
                                ModuleCommandClick, 
                                command
                            ));
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
            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

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
                CommandInterface? command = clickedItem.Tag as CommandInterface;
                if (command != null){
                    command.run();
                }
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



        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var hotKeyManager = ServiceLocator.GetService<HotKeyManager>();

            if (msg == WM_HOTKEY) 
            {
                hotKeyManager.hotKeyPressed(wParam.ToInt32());
            }
            return IntPtr.Zero;
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

            var settings = settingsManager.LoadSettings();
            if (settings.minimizeOnClose)
            {
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            var hotKeyManager = ServiceLocator.GetService<HotKeyManager>();
            var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
            var moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();

            var settings = settingsManager.LoadSettings();
            if (!settings.minimizeOnClose)
            {
                var source = PresentationSource.FromVisual(this) as HwndSource;
                source?.RemoveHook(WndProc);
                var modules = moduleManager.GetModules();
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
            var moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();

            var modules = moduleManager.GetModules();
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
            var moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();

            ImageSource icon = new BitmapImage(new Uri("pack://application:,,,/Resources/wrench.ico"));
            if (currentIconIndex != -1)
            {
                var modules = moduleManager.GetModules();
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
            var moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();

            string text = "Outfit Tool";

            var modules = moduleManager.GetModules();
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

    }
}