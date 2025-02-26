using OutfitTool.Common;
using OutfitTool.Services.HotkeyManager;
using System.Collections.Generic;

namespace OutfitTool.ModuleManager
{
    interface ModuleManagerInterface
    {
        List<Module> getModules();
        public void LoadModules(List<string>? enabledModules = null);
        public Module? GetModule(string name);
        public CommandDescriptor? GetCommandDescriptor(string commandString);
    }
}
