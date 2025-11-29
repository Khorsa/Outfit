using OutfitTool.ModulesAdapter.Dto;
using OutfitTool.Services.Updates;

namespace OutfitTool.View;

public class LastCompatibleRepositoryItem : RepositoryItem
{
    public bool CanInstall { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool HasInstalled { get; set; }
    public LastCompatibleRepositoryItemStatus Status { get; set; }

    public ModuleVersion? InstalledVersion { get; set; }

    public LastCompatibleRepositoryItem(): base(){}

    public LastCompatibleRepositoryItem(RepositoryItem item)
    {
        this.Name = item.Name;
        this.AssemblyName = item.AssemblyName;
        this.DisplayName = item.DisplayName;
        this.Description = item.Description;
        this.Version = item.Version;
        this.RequiredApi = item.RequiredApi;
        this.Changes = item.Changes;
        this.Author = item.Author;
        this.AuthorContacts = item.AuthorContacts;
        this.Status = LastCompatibleRepositoryItemStatus.Available;
        this.Url = item.Url;
    }
}