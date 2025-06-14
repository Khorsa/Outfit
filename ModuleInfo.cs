using OutfitTool.Common;

namespace OutfitTool
{
    class ModuleInfo : ModuleInfoInterface
    {
        public string Name => "outfit_tool";
        public string DisplayName => "Outfit Tool";
        public string AssemblyName => "OutfitTool";
        public string Description => "Outfit Tool";
        public ModuleVersion Version => new ModuleVersion(3, 0, "pre-alpha");
        public ModuleVersion Require => new ModuleVersion(3, 0);
        public string Changes => "3.0 Тестирование pre-alpha";
        public string Author => "Stolyarov Roman";
        public string AuthorContacts => "rshome@mail.ru";
    }
}
