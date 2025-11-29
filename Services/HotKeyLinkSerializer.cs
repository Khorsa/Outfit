using System.Windows.Input;
using OutfitTool.ModulesAdapter.ModuleManager;
using OutfitTool.Services.HotkeyManager;
using OutfitTool.Services.Settings;
using ICommand = OutfitTool.ModulesAdapter.ICommand;

namespace OutfitTool.Services;

static class HotKeyLinkSerializer
{
    public static string Serialize(HotKeyLink link)
    {
        string result =
            link.Key.KeyMod.ToString() +
            '+' +
            link.Key.KeyCode +
            '-' + 
            link.Command.Module.ModuleInfo.Name + 
            '.' + 
            link.Command.Command.Name;
        return result;
    }

    public static HotKeyLink Deserialize(string serialized)
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

        HotKey? key = null;
        if (
            Enum.TryParse(keyStrings[1], out Key k) &&
            Enum.TryParse(keyStrings[0], out ModifierKeys m)
        )
        {
            key = new HotKey(k, m);
        }

        if (key == null)
        {
            throw new Exception("Неверный формат сериализованной HotKeyLink " + serialized);
        }

        ModuleManager moduleManager = ServiceLocator.GetService<ModuleManager>();

        Module? module = moduleManager.GetModule(commandStrings[0]);
        if (module != null)
        {
            foreach(ICommand cmd in module.ModuleController.GetCommandList())
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