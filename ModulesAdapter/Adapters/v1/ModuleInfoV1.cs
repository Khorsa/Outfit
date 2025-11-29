using OutfitTool.ModulesAdapter.Dto;

namespace OutfitTool.ModulesAdapter.Adapters.v1;

public class ModuleInfoV1 : IModuleInfo
{
    OutfitTool.API1.IModuleInfo _api;

    public ModuleInfoV1(OutfitTool.API1.IModuleInfo api)
    {
        this._api = api;
    }

    public string Name => this._api.Name;

    public string DisplayName => this._api.DisplayName;

    public string AssemblyName => this._api.AssemblyName;

    public string Description => this._api.Description;

    public ModuleVersion Version => new ModuleVersion(_api.Version.MajorVersion, _api.Version.MinorVersion);

    public int RequireApiVersion => this._api.RequireApiVersion;

    public string Changes => this._api.Changes;

    public string Author => this._api.Author;

    public string AuthorContacts => this._api.AuthorContacts;

}