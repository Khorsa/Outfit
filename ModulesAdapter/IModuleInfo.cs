using OutfitTool.ModulesAdapter.Dto;

namespace OutfitTool.ModulesAdapter;

public interface IModuleInfo
{
    string Name { get; }
    string DisplayName { get; }
    string AssemblyName { get; }
    string Description { get; }
    ModuleVersion Version { get; }
    int RequireApiVersion { get; }
    string Changes { get; }
    string Author { get; }
    string AuthorContacts { get; }
}