using Microsoft.Win32;

namespace OutfitTool.Services;

class StartupManager
{
    private const string AUTORUN_REGISTRY_FOLDER = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string APP_NAME = "OutfitTool";

    public void AddToStartup()
    {
        var appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

        using var key = Registry.CurrentUser.OpenSubKey(StartupManager.AUTORUN_REGISTRY_FOLDER, true);
        key?.SetValue(StartupManager.APP_NAME, "\"" + appPath + "\"");
    }

    public void RemoveFromStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupManager.AUTORUN_REGISTRY_FOLDER, true);
        key?.DeleteValue(StartupManager.APP_NAME, false);
    }
}