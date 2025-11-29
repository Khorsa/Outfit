using OutfitTool.ModulesAdapter;
using OutfitTool.ModulesAdapter.ModuleManager;

namespace OutfitTool.Services.HotkeyManager;

public class CommandDescriptor
{
    public Module Module;
    public ICommand Command;

    public CommandDescriptor(
        Module module,
        ICommand command
    )
    {
        this.Module = module;
        this.Command = command;
    }

    public override string ToString()
    {
        return Module.AssemblyName + "." + Command.Name;
    }

    public static string[] GetModuleAndCommandNames(string name)
    {
        return name.Split('.');
    }
}