namespace OutfitTool.Services.Settings;

public class AppSettings
{
    public const string DEFAULT_UPDATES_REPOSITORY = "https://outfit-tool.ru/repository.yaml";

    public List<string> EnabledModules { get; set; } = []; // setter is needed and is used in SettingsManager
    public bool MinimizeOnClose = true;
    public bool MinimizeOnStart = true;
    public bool LoadOnSystemStart = true;
    public bool CheckUpdatesOnStart = true;
    public string UpdatesRepository = "https://outfit-tool.ru/repository.yaml";
    public bool InstallUpdates = false;
    public Dictionary<string,string> HotKeys { get; set; } = new();  // setter is needed and is used in SettingsManager

    public string Language = "ru-RU";
}