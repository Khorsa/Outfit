/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.Services;
using OutfitTool.Services.HotkeyManager;
using OutfitTool.Services.Settings;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Module = OutfitTool.ModulesAdapter.ModuleManager.Module;
using OutfitTool.ModulesAdapter.ModuleManager;
using OutfitTool.ModulesAdapter;
using OutfitTool.ModulesAdapter.Dto;
using OutfitTool.ModulesAdapter.Logger;

namespace OutfitTool.View;

public partial class MainWindow
{
    private const int WM_HOTKEY = 0x0312; // 786
    private const int ICON_BLINK_INTERVAL = 400;
    private static readonly DispatcherTimer IconTimer = new();
    public static MainWindow? Instance { get; private set; }

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();

        LocalizationHelper.LanguageChanged += LanguageChanged;

        InitIconTimer();
        InitModules();

        ServiceLocator.GetService<ILogger>().Subscribe(this.StatusLogHandler);
    }

    private void StatusLogHandler(object? sender, LogEntry logEntry)
    {
        if (logEntry.Level == LogLevel.Status)
        {
            StatusBarItemInfo.Content = logEntry.Message;
        }
    }

    private void InitIconTimer()
    {
        IconTimer.Interval = TimeSpan.FromMilliseconds(ICON_BLINK_INTERVAL);
        IconTimer.Tick += OnModulesTimedEvent;
        IconTimer.Start();
    }

    // Инициализация модулей
    private static void InitModules()
    {           
        var moduleManager = ServiceLocator.GetService<IModuleManager>();
        var settings = ServiceLocator.GetService<SettingsManager<AppSettings>>().LoadSettings();

        moduleManager.LoadModules(settings.EnabledModules);
        foreach (Module module in moduleManager.GetModules())
        {
            if (module.Enabled)
            {
                module.ModuleController.Init(ServiceLocator.GetService<ILogger>());
            }
        }
    }

    private void LanguageChanged(object? sender, EventArgs e)
    {
        InitInterface();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var hotKeyManager = ServiceLocator.GetService<HotKeyManager>();
        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

        hotKeyManager.Init(new WindowInteropHelper(this).Handle);
        InitInterface();
            

        var settings = settingsManager.LoadSettings();
        settingsManager.SaveSettings(settings);
        LocalizationHelper.Language = new CultureInfo(settings.Language);

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


        if (settings.MinimizeOnStart)
        {
            this.WindowState = WindowState.Minimized;
        }
    }



    public void InitInterface()
    {
        var moduleManager = ServiceLocator.GetService<IModuleManager>();
        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

        TaskbarIcon.Visibility = Visibility.Visible;

        var modules = moduleManager.GetModules();
        var settings = settingsManager.LoadSettings();

        // Установка языка
        foreach (Module module in modules)
        {
            module.ModuleController.SetLanguage(settings.Language);
        }

        // Инициализация контекстного меню
        TrayContextMenu.Items.Clear();
        foreach (Module module in modules)
        {
            if (module.Enabled)
            {
                foreach (ICommand command in module.ModuleController.GetCommandList())
                {
                    CommandContextMenu? menu = command.ContextMenu;
                    if (menu != null)
                    {
                        TrayContextMenu.Items.Add(CreateTrayMenuItem(
                            menu.Text, 
                            menu.Image == null ? null : new Image { Source = menu.Image, Width = 16, Height = 16 }, 
                            ModuleCommandClick, 
                            command
                        ));
                    }
                }
            }
        }
        TrayContextMenu.Items.Add(new Separator());

        TrayContextMenu.Items.Add(GetTrayMenuItem(LocalizationHelper.GetString("Settings"), "gear.png", OpenSettings));
        var languageItem = GetTrayMenuItem(LocalizationHelper.GetString("Language"), "font.png");
        foreach (var l in LocalizationHelper.Languages)
        {
            languageItem.Items.Add(LocalizationHelper.Language.ToString() == l.Value.Culture.ToString()
                ? GetTrayMenuItem("• " + l.Value.Name, l.Value.FlagResource, ChangeLanguage, l.Value.CultureName)
                : GetTrayMenuItem(l.Value.Name, l.Value.FlagResource, ChangeLanguage, l.Value.CultureName));
        }
        TrayContextMenu.Items.Add(languageItem);
        TrayContextMenu.Items.Add(GetTrayMenuItem(LocalizationHelper.GetString("Exit"), "navigate_cross.png", Exit_Click));
    }



    private static MenuItem CreateTrayMenuItem(string title, Image? image, RoutedEventHandler? onClick = null, object? tag = null)
    {
        var menuItem = new MenuItem
        {
            Header = title
        };
        if (onClick != null)
        {
            menuItem.Click += onClick;
        }
        if (tag != null)
        {
            menuItem.Tag = tag;
        }
        if (image != null)
        {
            menuItem.Icon = image;
        }
        return menuItem;
    }

    private MenuItem GetTrayMenuItem(string title, string? resource, RoutedEventHandler? onClick = null, object? tag = null)
    {
        Image? image = null;
        if (resource != null){
            BitmapImage icon = new BitmapImage(new Uri("pack://application:,,,/Resources/" + resource));
            image = new Image { Source = icon, Width = 16, Height = 16 };
        }
        return CreateTrayMenuItem(title, image, onClick, tag);
    }


    private void ChangeLanguage(object sender, RoutedEventArgs e)
    {
        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

        if (sender is MenuItem { Tag: string language })
        {
            var settings = settingsManager.LoadSettings();
            settings.Language = language;
            settingsManager.SaveSettings(settings);
            LocalizationHelper.Language = new CultureInfo(language);
        }
    }


    private static void ModuleCommandClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: ICommand command })
        {
            command.Run();
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
        this.ShutDownApplication();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (this.WindowState == WindowState.Minimized)
        {
            this.Hide();
        }
    }



    private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        var hotKeyManager = ServiceLocator.GetService<HotKeyManager>();

        if (msg == WM_HOTKEY) 
        {
            hotKeyManager.HotKeyPressed(wParam.ToInt32());
        }
        return IntPtr.Zero;
    }


    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();

        var settings = settingsManager.LoadSettings();
        if (settings.MinimizeOnClose)
        {
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        var settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
        var settings = settingsManager.LoadSettings();

        if (!settings.MinimizeOnClose)
        {
            ShutDownApplication();
        }
    }

    private void ShutDownApplication()
    {
        var hotKeyManager = ServiceLocator.GetService<HotKeyManager>();
        var moduleManager = ServiceLocator.GetService<IModuleManager>();

        var source = PresentationSource.FromVisual(this) as HwndSource;
        source?.RemoveHook(WndProc);
        var modules = moduleManager.GetModules();
        foreach (Module module in modules)
        {
            if (module.Enabled)
            {
                module.ModuleController.Shutdown();
            }
        }

        hotKeyManager.ClearAllKeys();
        Application.Current.Shutdown();
    }


    private int _currentIconIndex = -1;
    private void OnModulesTimedEvent(object? sender, EventArgs e)
    {
        TaskbarIcon.IconSource = this.GetTaskbarIcon();
        TaskbarIcon.ToolTipText = this.GetTaskbarIconText();
        CheckNotifications();
    }

    private Notification? _lastNotification;
    private void CheckNotifications()
    {
        var moduleManager = ServiceLocator.GetService<IModuleManager>();

        var modules = moduleManager.GetModules();
        foreach (Module module in modules)
        {
            if (module.Enabled)
            {
                var notification = module.ModuleController.PopNotification();
                if (notification != null)
                {
                    _lastNotification = notification;
                    TaskbarIcon.ShowBalloonTip(notification.Title, notification.Message, BalloonIcon.Info);
                }
            }
        }
    }

    private void ClickNotification(object sender, EventArgs e)
    {
        if (_lastNotification != null)
        {
            Process.Start(new ProcessStartInfo(_lastNotification.Href) { UseShellExecute = true });
        }
    }


    private ImageSource GetTaskbarIcon()
    {
        var moduleManager = ServiceLocator.GetService<IModuleManager>();

        ImageSource icon = new BitmapImage(new Uri("pack://application:,,,/Resources/wrench.ico"));
        if (_currentIconIndex != -1)
        {
            var modules = moduleManager.GetModules();
            var imageSources = new List<BitmapImage>();
            foreach (Module module in modules)
            {
                if (module.Enabled)
                {
                    var i = module.ModuleController.GetTaskbarIcon();
                    if (i != null)
                    {
                        imageSources.Add(i);
                    }
                }
            }

            if (_currentIconIndex >= 0 && _currentIconIndex < imageSources.Count())
            {
                icon = imageSources[_currentIconIndex];
            }

            _currentIconIndex++;
            if (_currentIconIndex >= imageSources.Count())
            {
                _currentIconIndex = -1;
            }
        }
        else
        {
            _currentIconIndex = 0;
        }

        return icon;
    }

    private string GetTaskbarIconText()
    {
        var moduleManager = ServiceLocator.GetService<IModuleManager>();

        string text = "Outfit Tool";

        var modules = moduleManager.GetModules();
        foreach (Module module in modules)
        {
            if (module.Enabled)
            {
                string? moduleTaskbarIconText = module.ModuleController.GetTaskbarIconText();
                if (moduleTaskbarIconText != null) {
                    text = text + "\r\n" + module.ModuleInfo.Name + ": " + moduleTaskbarIconText;
                }
            }
        }

        return text;
    }

}