using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
