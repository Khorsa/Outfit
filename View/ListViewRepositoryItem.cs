using OutfitTool.Services.Updates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutfitTool.View
{
    class ListViewRepositoryItem
    {
        public string Name;
        public string Versions;

        public ListViewRepositoryItem(string Name, string Versions)
        {
            this.Name = Name;
            this.Versions = Versions;
        }
        public static List<ListViewRepositoryItem> MapRepository(List<RepositoryItem> RepositoryItem)
        {
            var versions = new Dictionary<string, List<string>>();
            foreach (RepositoryItem Item in RepositoryItem)
            {
                if (! versions.ContainsKey(Item.Name))
                {
                    versions.Add(Item.Name, new List<string>());
                }
                versions[Item.Name].Add(Item.Version.ToString());
            }

            var list = new List<ListViewRepositoryItem>();
            foreach (var version in versions) { 
                list.Add(new ListViewRepositoryItem(version.Key, string.Join(", ", version.Value)));
            }

            return list;
        }
    }

}
