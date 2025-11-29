using OutfitTool.ModulesAdapter;
using OutfitTool.ModulesAdapter.ModuleManager;

namespace OutfitTool.View;

public class ListViewCommandItem
{
    public ListViewCommandItem(ICommand command, Module module, string hotKey)
    {
        this.Command = command;
        this.Module = module;
        this.HotKey = hotKey;
    }

    public ICommand Command { get; set; }
    public Module Module { get; set; }
    public string HotKey { get; set; }
}