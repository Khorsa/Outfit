using OutfitTool.API1;
using OutfitTool.ModulesAdapter.Adapters.v1;

namespace OutfitTool.ModulesAdapter.Adapters;

public class ModuleInfoAdapter(Type type)
{
    public IModuleInfo? Get()
    {
        API1.IModuleInfo? info = null;
        if (type.IsAssignableTo(typeof(API1.IModuleInfo)))
        {
            info = Activator.CreateInstance(type) as API1.IModuleInfo;
        }

        if (info != null) return new ModuleInfoV1(info);

        // Проверяем другие версии API...
        return null;

    }
}