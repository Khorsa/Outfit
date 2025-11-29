using OutfitTool.ModulesAdapter.Adapters.v1;

namespace OutfitTool.ModulesAdapter.Adapters;

public class ModuleControllerAdapter(Type type)
{
    public IModuleController? Get()
    {
        API1.IModuleController? controller = null;
        if (type.IsAssignableTo(typeof(API1.IModuleController)))
        {
            controller = Activator.CreateInstance(type) as API1.IModuleController;
        }
        if (controller != null) return new ModuleControllerV1(controller);
            
        // Проверяем другие версии API...
        return null;
    }
}