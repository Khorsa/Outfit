using Common.Logger;
using OutfitTool.ModuleManager;
using OutfitTool.Services.Settings;
using System.Windows;

namespace OutfitTool.Services.HotkeyManager
{
    internal class HotKeyManager(SettingsManager<AppSettings> settingsManager)
    {
        private HotKeyRegisterService? hotKeyRegisterService = null;
        private readonly SettingsManager<AppSettings> settingsManager = settingsManager;

        public void init(IntPtr windowHandle)
        {
            this.hotKeyRegisterService = new HotKeyRegisterService(windowHandle);

            AppSettings settings = settingsManager.LoadSettings();

            foreach (KeyValuePair<string,string> entry in settings.hotKeys)
            {
                string commandString = entry.Key;
                string hotKey = entry.Value;

                if (commandString == null)
                {
                    return;
                }

                ModuleManagerInterface moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();
                CommandDescriptor? commandDescriptor = moduleManager.GetCommandDescriptor(commandString);

                HotKey? key = HotKey.FromString(hotKey);

                if (key != null && commandDescriptor != null) {

                    var logger = ServiceLocator.GetService<LoggerInterface>();
                    logger.Info("Регистрация горячей клавиши " + key.ToString());

                    clearKey(commandDescriptor);
                    registerKey(commandDescriptor, key);
                }
            }
        }


        public void registerKey(CommandDescriptor commandDescriptor, HotKey key)
        {
            AppSettings settings = settingsManager.LoadSettings();

            try
            {
                hotKeyRegisterService.Register(key);
                settings.hotKeys.Remove(commandDescriptor.ToString());
                settings.hotKeys.Add(commandDescriptor.ToString(), key.ToString());
                settingsManager.SaveSettings(settings);
            }
            catch (Exception e) {
                System.Windows.MessageBox.Show(e.Message, "Ошибка регистрации горячих клавиш", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        public void clearKey(CommandDescriptor commandDescriptor)
        {
            AppSettings settings = settingsManager.LoadSettings();

            HotKey? key = getKey(commandDescriptor);

            if (key != null)
            {
                hotKeyRegisterService.Unregister(key);
                settings.hotKeys.Remove(commandDescriptor.ToString());
            }

            settingsManager.SaveSettings(settings);
        }

        public void clearAllKeys()
        {
            AppSettings settings = settingsManager.LoadSettings();

            foreach (KeyValuePair<string, string> entry in settings.hotKeys)
            {
                HotKey? hotKey = HotKey.FromString(entry.Value.Trim());
                if(hotKey is HotKey){
                    hotKeyRegisterService.Unregister(hotKey);
                }
            }
        }


        public HotKey? getKey(CommandDescriptor command)
        {
            AppSettings settings = settingsManager.LoadSettings();

            foreach (KeyValuePair<string, string> entry in settings.hotKeys)
            {
                string commandString = entry.Key.Trim();
                string hotKeyString = entry.Value.Trim();

                if (command.ToString() == commandString)
                {
                    return HotKey.FromString(hotKeyString);
                }

            }
            return null;
        }


        public void hotKeyPressed(int hotKeyId)
        {
            AppSettings settings = settingsManager.LoadSettings();

            HotKey? key = hotKeyRegisterService.getById(hotKeyId);

            if (key == null)
            {
                return;
            }

            string? commandString = null;

            foreach(KeyValuePair<string, string> entry in settings.hotKeys)
            {
                if (entry.Value == key.ToString())
                {
                    commandString = entry.Key.Trim();
                }
            }

            if (commandString == null)
            {
                return;
            }

            ModuleManagerInterface moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();
            CommandDescriptor? command = moduleManager.GetCommandDescriptor(commandString);

            if (command != null)
            {
                command.command.run();
            }
        }
    }
}
