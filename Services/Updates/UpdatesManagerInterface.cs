using static OutfitTool.Services.Updates.UpdatesManager;

namespace OutfitTool.Services.Updates;

interface IUpdatesManagerInterface
{
    public void LoadList(LoadListHandler updateManagerCheckStatusChanged);
    public void UpdateAll();
    public void Download(RepositoryItem item);
    public void DeleteModule(RepositoryItem module);
}