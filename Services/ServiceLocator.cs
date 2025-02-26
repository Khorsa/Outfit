using Common.Logger;
using OutfitTool.Services.HotkeyManager;
using OutfitTool.Services.Settings;
using OutfitTool.Services.Updates;

namespace OutfitTool.Services
{
    internal class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new();

        public static void RegisterServices()
        {
            Register<LoggerInterface>(() => new Logger());
            Register<ModuleManager.ModuleManagerInterface>(() => new ModuleManager.ModuleManager(GetService<LoggerInterface>()));
            Register(() => new SettingsManager<AppSettings>());
            Register(() => new StartupManager());
            Register(() => new UpdatesManager());
            Register(() => new HotKeyManager(GetService<SettingsManager<AppSettings>>()));
        }
        public static T GetService<T>() where T : class
        {
            var service = TryToGetService<T>();
            if (service == null)
            {
                throw new InvalidOperationException($"Service of type {typeof(T)} not registered.");
            }
            return service;
        }

        private static void Register<T>(Func<T> factory) where T : class
        {
            if (TryToGetService<T>() == null)
            {
                services[typeof(T)] = factory();
            }
        }

        private static T? TryToGetService<T>() where T : class
        {
            return services.TryGetValue(typeof(T), out var service) ? service as T : null;
        }
    }
}
