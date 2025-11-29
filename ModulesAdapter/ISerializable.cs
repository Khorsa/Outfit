namespace OutfitTool.ModulesAdapter;

public interface IStringSerializable
{
    public string ToString();
    public IStringSerializable FromString(string input);
}