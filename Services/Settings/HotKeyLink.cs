using Common;
using OutfitTool.Services.HotkeyManager;

namespace OutfitTool.Services.Settings
{
    public class HotKeyLink: IStringSerializable
    {
        public HotKeyLink(HotKey Key = null, CommandDescriptor Command = null)
        {
            this.Key = Key;
            this.Command = Command;
        }

        public HotKeyLink()
        {
        }


        public HotKey Key { get; set; }
        public CommandDescriptor Command { get; set; }

        public IStringSerializable FromString(string input)
        {
            return HotKeyLinkSerializer.deserialize(input);
        }

        public override string ToString()
        {
            return HotKeyLinkSerializer.serialize(this);
        }
    }
}
