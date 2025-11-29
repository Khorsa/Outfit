using System.Windows.Media.Imaging;
using OutfitTool.ModulesAdapter.Dto;
using OutfitTool.ModulesAdapter.Logger;

namespace OutfitTool.ModulesAdapter;

public interface IModuleController
{
    public void SetLanguage(string language);
    public void Init(ILogger logger);
    public void Shutdown();
    List<ICommand> GetCommandList();
    public BitmapImage? GetTaskbarIcon();
    public string? GetTaskbarIconText();
    public Notification? PopNotification();
    public void OpenSettings();
}