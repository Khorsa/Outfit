using OutfitTool.ModulesAdapter;
using OutfitTool.ModulesAdapter.Logger;
using OutfitTool.Services;
using System.Windows;
using System.Windows.Threading;
using OutfitTool.Services.Updates;

namespace OutfitTool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private readonly SingleInstance _singleInstance = new();

    public static ModuleInfo ModuleInfo { get; private set; } = null!;

    public App() : base()
    {
        ModuleInfo = new ModuleInfo();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Сначала параметры: FilesUpdater создаётся ServiceLocator-ом сразу и должен
        // получить install-dir из --install-dir, а не BaseDirectory кэша.
        StartParameters.FillFromCommandLineArguments(e);
        ServiceLocator.RegisterServices();

        if (!StartParameters.GetOne<bool>("update_mode"))
        {
            // Обычный запуск: один экземпляр на сессию, затем очистка временных каталогов.
            if (!_singleInstance.TryAcquire())
            {
                Shutdown();
                Environment.Exit(0);
            }

            ServiceLocator.GetService<FilesUpdater>().CleanupCache();
            return;
        }

        // Update mode: файлы уже скачаны, установка не должна пострадать при ошибке.
        var updater = ServiceLocator.GetService<FilesUpdater>();
        var success = updater.Update(StartParameters.ModulesToDelete, StartParameters.GetAll());

        if (!success && !Environment.HasShutdownStarted)
        {
            MessageBox.Show(
                "Не удалось установить обновление. Предыдущая версия сохранена; " +
                "подробности записаны в лог. Приложение будет закрыто.",
                "Ошибка обновления",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        Environment.Exit(success ? 0 : 1);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance.Dispose();
        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("Dispatcher", e.Exception);

        try
        {
            MessageBox.Show(
                "Произошла непредвиденная ошибка. Подробности записаны в лог.",
                "OutfitTool",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // Если показать сообщение не удалось, не роняем обработчик.
        }

        // Не даём одному исключению UI-потока закрыть всё приложение.
        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogException("AppDomain", e.ExceptionObject as Exception);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("Task", e.Exception);
        e.SetObserved();
    }

    private static void LogException(string context, Exception? exception)
    {
        try
        {
            ServiceLocator.GetService<ILogger>().Error($"[{context}] {exception}");
        }
        catch
        {
            // Логгер ещё не зарегистрирован или сам упал — не создаём рекурсию.
            System.Diagnostics.Debug.WriteLine($"[{context}] {exception}");
        }
    }
}
