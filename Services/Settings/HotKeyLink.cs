using OutfitTool.ModulesAdapter;
using OutfitTool.Services.HotkeyManager;

namespace OutfitTool.Services.Settings;

public class HotKeyLink(HotKey key, CommandDescriptor command) : IStringSerializable
{
    public HotKey Key { get; } = key;
    public CommandDescriptor Command { get; } = command;

    public IStringSerializable FromString(string input)
    {
        return HotKeyLinkSerializer.Deserialize(input);
    }

    public override string ToString()
    {
        return HotKeyLinkSerializer.Serialize(this);
    }
}