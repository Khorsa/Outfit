namespace OutfitTool.ModulesAdapter.Dto;

public class Notification(string title, string message, string href)
{
    public readonly string Message = message;
    public readonly string Title = title;
    public readonly string Href = href;
}