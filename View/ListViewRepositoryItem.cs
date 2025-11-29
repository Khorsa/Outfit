using OutfitTool.Services.Updates;

namespace OutfitTool.View;

class ListViewRepositoryItem
{
    public string Name;
    public string Versions;

    public ListViewRepositoryItem(string name, string versions)
    {
        this.Name = name;
        this.Versions = versions;
    }
    public static List<ListViewRepositoryItem> MapRepository(List<RepositoryItem> repositoryItem)
    {
        var versions = new Dictionary<string, List<string>>();
        foreach (RepositoryItem item in repositoryItem)
        {
            if (! versions.ContainsKey(item.Name))
            {
                versions.Add(item.Name, new List<string>());
            }
            versions[item.Name].Add(item.Version.ToString());
        }

        var list = new List<ListViewRepositoryItem>();
        foreach (var version in versions) { 
            list.Add(new ListViewRepositoryItem(version.Key, string.Join(", ", version.Value)));
        }

        return list;
    }
}