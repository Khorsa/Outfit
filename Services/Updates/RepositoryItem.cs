using OutfitTool.ModulesAdapter.Dto;

namespace OutfitTool.Services.Updates;

public class RepositoryItem
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string AssemblyName { get; set; }
    public string Description { get; set; }
    public ModuleVersion Version { get; set; }
    public int RequiredApi { get; set; }
    public List<int> SuppertedApi { get; set; }
    public string Changes { get; set; }

    public string Author { get; set; }
    public string AuthorContacts { get; set; }

    public string Url { get; set; }

    public RepositoryItem()
    {
        Name = "";
        DisplayName = "";
        Changes = "";
        Version = new ModuleVersion(0, 0);
        RequiredApi = 1;
        SuppertedApi = [1, 2];
        Description = "Тут - описание модуля";
        Url = "";
        Author = "";
        AuthorContacts = "";
        AssemblyName = "";
    }

    public override string ToString()
    {
        string res = Name + " " + Version.ToString();
        if (Version.VersionLabel != "")
        {
            res += " " + Version.VersionLabel;
        }
        return res;
    }
}