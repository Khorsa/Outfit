namespace OutfitTool.ModulesAdapter.ModuleManager;

public class Module(
    string assemblyName,
    IModuleInfo moduleInfo,
    IModuleController moduleController)
{
    public IModuleInfo ModuleInfo { get; } = moduleInfo;
    public IModuleController ModuleController { get; } = moduleController;
    public string AssemblyName { get; } = assemblyName;
    public bool Enabled { get; set; }
}