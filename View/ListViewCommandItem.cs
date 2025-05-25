using OutfitTool.Common;
using OutfitTool.ModuleManager;

namespace OutfitTool.View
{
    internal class ListViewCommandItem
    {
        public ListViewCommandItem(CommandInterface command, Module module, string hotKey)
        {
            this.command = command;
            this.module = module;
            this.hotKey = hotKey;
        }

        public CommandInterface command { get; set; }
        public Module module { get; set; }
        public string hotKey { get; set; }
    }
}
