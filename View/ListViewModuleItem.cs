using OutfitTool.ModulesAdapter.ModuleManager;
using OutfitTool.Services;

namespace OutfitTool.View;

public class ListViewModuleItem
{
    public ListViewModuleItem(Module module)
    {
        this.Module = module;
        this.Name = module.ModuleInfo.Name;
        this.State = module.Enabled ? LocalizationHelper.GetString("On") : LocalizationHelper.GetString("Off");
    }

    public Module Module { get; set; }
    public string Name { get; set; }
    public string State { get; set; }
}