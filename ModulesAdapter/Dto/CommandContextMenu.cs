using System.Windows.Media.Imaging;

namespace OutfitTool.ModulesAdapter.Dto;

public class CommandContextMenu(string text, BitmapImage image)
{
    public string Text { get; set; } = text;
    public BitmapImage Image { get; set; } = image;
}