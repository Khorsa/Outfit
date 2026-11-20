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
    private const int ICON_BLINK_INTERVAL = 500;
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
        var logger = ServiceLocator.GetService<ILogger>();

        moduleManager.LoadModules(settings.EnabledModules);
        foreach (Module module in moduleManager.GetModules())
        {
            if (!module.Enabled) continue;

            try
            {
                module.ModuleController.Init(logger);
            }
            catch (Exception ex)
            {
                logger.Error($"Не удалось инициализировать модуль {module.ModuleInfo.Name}: {ex}");
                module.Enabled = false;
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

        try
        {
            hotKeyManager.Init(new WindowInteropHelper(this).Handle);
        }
        catch (Exception ex)
        {
            ServiceLocator.GetService<ILogger>().Error($"Ошибка инициализации горячих клавиш: {ex}");
        }

        InitInterface();

        var settings = settingsManager.LoadSettings();

        try
        {
            LocalizationHelper.Language = new CultureInfo(settings.Language);
        }
        catch (Exception ex)
        {
            ServiceLocator.GetService<ILogger>().Warning($"Не удалось применить язык '{settings.Language}': {ex.Message}");
            LocalizationHelper.Language = new CultureInfo("ru-RU");
        }

        var source = PresentationSource.FromVisual(this) as HwndSource;
        source?.AddHook(WndProc);

        if (StartParameters.TryGetOne("selected_tab", out int selectedTab)
            && selectedTab >= 0
            && selectedTab < TabControl.Items.Count)
        {
            TabControl.SelectedIndex = selectedTab;
        }

        // Устанавливаем переданные размеры и положение окна.
        if (StartParameters.TryGetOne("left", out int left)) this.Left = left;
        if (StartParameters.TryGetOne("top", out int top)) this.Top = top;
        if (StartParameters.TryGetOne("width", out int width) && width > 0) this.Width = width;
        if (StartParameters.TryGetOne("height", out int height) && height > 0) this.Height = height;

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
        var logger = ServiceLocator.GetService<ILogger>();

        // Установка языка
        foreach (Module module in modules)
        {
            try
            {
                module.ModuleController.SetLanguage(settings.Language);
            }
            catch (Exception ex)
            {
                logger.Warning($"Модуль {module.ModuleInfo.Name}: ошибка SetLanguage: {ex.Message}");
            }
        }

        // Инициализация контекстного меню
        TrayContextMenu.Items.Clear();
        foreach (Module module in modules)
        {
            if (!module.Enabled) continue;

            try
            {
                foreach (ICommand command in module.ModuleController.GetCommandList())
                {
                    CommandContextMenu? menu = command.ContextMenu;
                    if (menu == null) continue;

                    TrayContextMenu.Items.Add(CreateTrayMenuItem(
                        menu.Text, 
                        menu.Image == null ? null : new Image { Source = menu.Image, Width = 16, Height = 16 }, 
                        ModuleCommandClick, 
                        command
                    ));
                }
            }
            catch (Exception ex)
            {
                logger.Warning($"Модуль {module.ModuleInfo.Name}: ошибка построения меню: {ex.Message}");
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
            try
            {
                command.Run();
            }
            catch (Exception ex)
            {
                ServiceLocator.GetService<ILogger>().Error(
                    $"Ошибка выполнения команды '{command.Name}': {ex}");
                MessageBox.Show(
                    $"Команда '{command.Name}' завершилась ошибкой. Подробности в логе.",
                    "OutfitTool",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
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
        if (msg != WM_HOTKEY) return IntPtr.Zero;

        try
        {
            var hotKeyManager = ServiceLocator.GetService<HotKeyManager>();
            hotKeyManager.HotKeyPressed(wParam.ToInt32());
        }
        catch (Exception ex)
        {
            ServiceLocator.GetService<ILogger>().Error($"Ошибка обработки горячей клавиши: {ex}");
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
        var logger = ServiceLocator.GetService<ILogger>();

        var source = PresentationSource.FromVisual(this) as HwndSource;
        source?.RemoveHook(WndProc);
        var modules = moduleManager.GetModules();
        foreach (Module module in modules)
        {
            if (!module.Enabled) continue;

            try
            {
                module.ModuleController.Shutdown();
            }
            catch (Exception ex)
            {
                logger.Warning($"Модуль {module.ModuleInfo.Name}: ошибка Shutdown: {ex.Message}");
            }
        }

        try
        {
            hotKeyManager.ClearAllKeys();
        }
        catch (Exception ex)
        {
            logger.Warning($"Ошибка очистки горячих клавиш: {ex.Message}");
        }

        Application.Current.Shutdown();
    }


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
        var logger = ServiceLocator.GetService<ILogger>();

        var modules = moduleManager.GetModules();
        foreach (Module module in modules)
        {
            if (!module.Enabled) continue;

            try
            {
                var notification = module.ModuleController.PopNotification();
                if (notification != null)
                {
                    _lastNotification = notification;
                    TaskbarIcon.ShowBalloonTip(notification.Title, notification.Message, BalloonIcon.Info);
                }
            }
            catch (Exception ex)
            {
                logger.Warning($"Модуль {module.ModuleInfo.Name}: ошибка PopNotification: {ex.Message}");
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


    private int _iconBlinkIndex = -1;
    private ImageSource? _defaultTrayIcon;

    private ImageSource DefaultTrayIcon =>
        _defaultTrayIcon ??= new BitmapImage(new Uri("pack://application:,,,/Resources/wrench.ico"));

    private ImageSource GetTaskbarIcon()
    {
        var moduleManager = ServiceLocator.GetService<IModuleManager>();
        var logger = ServiceLocator.GetService<ILogger>();

        var moduleIcons = new List<ImageSource>();
        foreach (Module module in moduleManager.GetModules())
        {
            if (!module.Enabled) continue;

            try
            {
                var icon = module.ModuleController.GetTaskbarIcon();
                if (icon != null) moduleIcons.Add(icon);
            }
            catch (Exception ex)
            {
                logger.Warning($"Модуль {module.ModuleInfo.Name}: ошибка GetTaskbarIcon: {ex.Message}");
            }
        }

        // Нет активных иконок модулей — показываем обычную иконку без мигания.
        if (moduleIcons.Count == 0)
        {
            _iconBlinkIndex = -1;
            return DefaultTrayIcon;
        }

        // Мигаем: основная иконка → иконки модулей → основная иконка...
        // Это индикатор внимания (например, непрочитанное письмо), а не смена темы.
        var sequence = new List<ImageSource>(moduleIcons.Count + 1) { DefaultTrayIcon };
        sequence.AddRange(moduleIcons);

        _iconBlinkIndex = (_iconBlinkIndex + 1) % sequence.Count;
        return sequence[_iconBlinkIndex];
    }

    private string GetTaskbarIconText()
    {
        var moduleManager = ServiceLocator.GetService<IModuleManager>();
        var logger = ServiceLocator.GetService<ILogger>();

        string text = "Outfit Tool";

        var modules = moduleManager.GetModules();
        foreach (Module module in modules)
        {
            if (!module.Enabled) continue;

            try
            {
                string? moduleTaskbarIconText = module.ModuleController.GetTaskbarIconText();
                if (moduleTaskbarIconText != null)
                {
                    text = text + "\r\n" + module.ModuleInfo.Name + ": " + moduleTaskbarIconText;
                }
            }
            catch (Exception ex)
            {
                logger.Warning($"Модуль {module.ModuleInfo.Name}: ошибка GetTaskbarIconText: {ex.Message}");
            }
        }

        return text;
    }

}