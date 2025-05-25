using System.Collections.Generic;
using System.Linq;

namespace OutfitTool.Services.Updates
{
    class RepositoryCollection
    {
        private List<RepositoryItem> repositoryItems = new List<RepositoryItem>();
        public void Add(RepositoryItem repositoryItem)
        {
            repositoryItems.Add(repositoryItem);
        }

        public void Remove(RepositoryItem repositoryItem) 
        { 
            repositoryItems.Remove(repositoryItem);
        }
        public List<RepositoryItem> Get()
        {
            return repositoryItems;
        }
        public List<RepositoryItem> Get(string moduleName)
        {
            var result = new List<RepositoryItem>();
            foreach (var item in repositoryItems)
            {
                if (item.Name == moduleName)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        public List<RepositoryItem> GetLastVersionModules()
        {
            var items = new Dictionary<string, RepositoryItem>();
            foreach (var item in repositoryItems)
            {
                if (!items.TryGetValue(item.AssemblyName, out var existingItem) || item.Version > existingItem.Version)
                {
                    items[item.AssemblyName] = item;
                }
            }
            return items.Values.ToList();
        }


        public List<string> GetModulesNames()
        {
            var result = new List<string>();
            foreach (var item in repositoryItems)
            {
                if (!result.Contains(item.Name))
                {
                    result.Add(item.Name);
                }
            }
            return result;
        }


        public void Clear()
        {
            repositoryItems.Clear();
        }
        public bool Contains(RepositoryItem item) 
        { 
            return repositoryItems.Contains(item);
        }

    }
}
