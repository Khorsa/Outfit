using OutfitTool.ModulesAdapter;
using OutfitTool.ModulesAdapter.Dto;

namespace OutfitTool;

public class ModuleInfo : IModuleInfo
{
    public string Name => "outfit_tool";
    public string DisplayName => "Outfit Tool";
    public string AssemblyName => "OutfitTool";
    public string Description => "Outfit Tool";
    public ModuleVersion Version => new(3, 0, "alpha");
    ModuleVersion IModuleInfo.Version => throw new NotImplementedException();
    public string Changes => "3.0 Тестирование pre-alpha";
    public string Author => "Stolyarov Roman";
    public string AuthorContacts => "rshome@mail.ru";
    public int RequireApiVersion => 2;

    public readonly int[] SupportedApiVersions = [1];
}