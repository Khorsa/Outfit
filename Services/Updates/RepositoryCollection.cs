namespace OutfitTool.Services.Updates;

class RepositoryCollection
{
    private List<RepositoryItem> _repositoryItems = new List<RepositoryItem>();
    public void Add(RepositoryItem repositoryItem)
    {
        _repositoryItems.Add(repositoryItem);
    }

    public void Remove(RepositoryItem repositoryItem) 
    { 
        _repositoryItems.Remove(repositoryItem);
    }
    public List<RepositoryItem> Get()
    {
        return _repositoryItems;
    }
    public List<RepositoryItem> Get(string moduleName)
    {
        var result = new List<RepositoryItem>();
        foreach (var item in _repositoryItems)
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
        foreach (var item in _repositoryItems)
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
        foreach (var item in _repositoryItems)
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
        _repositoryItems.Clear();
    }
    public bool Contains(RepositoryItem item) 
    { 
        return _repositoryItems.Contains(item);
    }

}