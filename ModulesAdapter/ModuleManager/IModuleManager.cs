using OutfitTool.Services.HotkeyManager;

namespace OutfitTool.ModulesAdapter.ModuleManager;

interface IModuleManager
{
    List<Module> GetModules();
    public void LoadModules(List<string>? enabledModules = null);
    public Module? GetModule(string name);
    public CommandDescriptor? GetCommandDescriptor(string commandString);
}