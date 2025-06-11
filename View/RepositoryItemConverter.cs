using OutfitTool.ModuleManager;
using OutfitTool.Services;
using OutfitTool.Services.Updates;

namespace OutfitTool.View
{
    class RepositoryItemConverter
    {
        public RepositoryItemConverter() { }

        public List<LastCompatibleRepositoryItem> ConvertCollection(RepositoryCollection collection)
        {
            var lastVersionsDict = new Dictionary<string, LastCompatibleRepositoryItem>();

            var moduleManager = ServiceLocator.GetService<ModuleManagerInterface>();

            foreach (var item in collection.Get())
            {
                if (App.ModuleInfo.Version < item.Require)
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
                var installed = moduleManager.GetModule(item.Name);

                var resultItem = new LastCompatibleRepositoryItem(item);

                if (item.AssemblyName == App.ModuleInfo.AssemblyName)
                {
                    // Это основной модуль
                    resultItem.InstalledVersion = App.ModuleInfo.Version;
                    resultItem.HasInstalled = true;
                    resultItem.CanInstall = false;
                    resultItem.CanDelete = false;
                    resultItem.CanUpdate = (installed != null && installed.moduleInfo.Version < item.Version);
                }
                else
                {
                    resultItem.HasInstalled = (installed != null);
                    resultItem.CanInstall = (installed == null);
                    resultItem.CanDelete = (installed != null);
                    resultItem.CanUpdate = (installed != null && installed.moduleInfo.Version < item.Version);
                    resultItem.InstalledVersion = installed?.moduleInfo.Version;
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
}
