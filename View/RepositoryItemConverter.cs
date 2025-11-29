using OutfitTool.ModulesAdapter.ModuleManager;
using OutfitTool.Services;
using OutfitTool.Services.Updates;

namespace OutfitTool.View;

class RepositoryItemConverter
{
    public RepositoryItemConverter() { }

    public List<LastCompatibleRepositoryItem> ConvertCollection(RepositoryCollection collection)
    {
        var lastVersionsDict = new Dictionary<string, LastCompatibleRepositoryItem>();

        var moduleManager = ServiceLocator.GetService<IModuleManager>();

        foreach (var item in collection.Get())
        {
            if (!App.ModuleInfo.SupportedApiVersions.Contains<int>(item.RequiredApi))
            {
                // Убираем несовместимые версии
                continue;
            }

            // Находим последнюю версию
            if (lastVersionsDict.ContainsKey(item.Name))
            {
                if (item.Version < lastVersionsDict[item.Name].Version)
                {
                    continue;
                }
                lastVersionsDict.Remove(item.Name);
            }

            var resultItem = new LastCompatibleRepositoryItem(item);

            if (item.AssemblyName == App.ModuleInfo.AssemblyName)
            {
                // Это основной модуль
                if (item.Version < App.ModuleInfo.Version) {
                    continue;
                }
                resultItem.InstalledVersion = App.ModuleInfo.Version;
                resultItem.HasInstalled = true;
                resultItem.CanInstall = false;
                resultItem.CanDelete = false;
                resultItem.CanUpdate = (App.ModuleInfo.Version < item.Version);
            }
            else
            {
                var installed = moduleManager.GetModule(item.Name);
                if (installed != null && item.Version < installed.ModuleInfo.Version)
                {
                    continue;
                }
                resultItem.HasInstalled = (installed != null);
                resultItem.CanInstall = (installed == null);
                resultItem.CanDelete = (installed != null);
                resultItem.CanUpdate = (installed != null && installed.ModuleInfo.Version < item.Version);
                resultItem.InstalledVersion = installed?.ModuleInfo.Version;
            }
            resultItem.Status = resultItem.CanUpdate ?
                LastCompatibleRepositoryItemStatus.Available :
                resultItem.HasInstalled ?
                    LastCompatibleRepositoryItemStatus.Installed :
                    LastCompatibleRepositoryItemStatus.NotAvailable;

            lastVersionsDict.Add(item.Name, resultItem);
        }
        var result =  new List<LastCompatibleRepositoryItem>();
        foreach (var item in lastVersionsDict) {
            result.Add(item.Value);
        }
        return result;
    }
}