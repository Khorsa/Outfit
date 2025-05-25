using OutfitTool.Services.HotkeyManager;
using OutfitTool.Services.Settings;
using OutfitTool.ModuleManager;
using OutfitTool.Common;
using System.Windows.Input;

namespace OutfitTool.Services
{
    class HotKeyLinkSerializer
    {
        public static string serialize(HotKeyLink link)
        {
            string result =
                link.Key.keyMod.ToString() +
                '+' +
                link.Key.keyCode +
                '-' + 
                link.Command.module.moduleInfo.Name + 
                '.' + 
                link.Command.command.Name;
            return result;
        }

        public static HotKeyLink deserialize(string serialized)
        {
            string[] keyAndCommandStrings = serialized.Split('-');
            if (keyAndCommandStrings.Length != 2)
            {
                throw new Exception("Неверный формат сериализованной HotKeyLink " + serialized);
            }
            string[] keyStrings = keyAndCommandStrings[0].Split('+');
            string[] commandStrings = keyAndCommandStrings[1].Split('.');
            if (keyStrings.Length != 2 || commandStrings.Length != 2)
            {
                throw new Exception("Неверный формат сериализованной HotKeyLink " + serialized);
            }

            Key k;
            ModifierKeys m;
            HotKey? key = null;
            if (
                Enum.TryParse<Key>(keyStrings[1], out k) &&
                Enum.TryParse<ModifierKeys>(keyStrings[0], out m)
            )
            {
                key = new HotKey(k, m);
            }

            if (key == null)
            {
                throw new Exception("Неверный формат сериализованной HotKeyLink " + serialized);
            }

            ModuleManager.ModuleManager moduleManager = ServiceLocator.GetService<ModuleManager.ModuleManager>();

            Module? module = moduleManager.GetModule(commandStrings[0]);
            if (module != null)
            {
                foreach(CommandInterface cmd in module.moduleController.getCommandList())
                {
                    if (cmd.Name == commandStrings[1])
                    {
                        return new HotKeyLink(key, new CommandDescriptor(module, cmd));
                    }
                }
            }

            throw new Exception("Не найдена команда " + commandStrings[0] + '.' + commandStrings[1]);
        }
    }
}
