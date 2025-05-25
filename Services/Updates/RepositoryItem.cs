using OutfitTool.Common;

namespace OutfitTool.Services.Updates
{
    internal class RepositoryItem
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string AssemblyName { get; set; }
        public string Description { get; set; }
        public ModuleVersion Version { get; set; }
        public ModuleVersion Require { get; set; }
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
            Require = new ModuleVersion(0, 0);
            Description = "Тут - описание модуля";
            Url = "";
            Author = "";
            AuthorContacts = "";
            AssemblyName = "";
        }

        public override string ToString()
        {
            return Name + " " + Version.ToString();
        }
    }
}
