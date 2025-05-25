using Microsoft.Win32;

namespace OutfitTool.Services
{
    class StartupManager
    {
        private const string AUTORUN_REGISTRY_FOLDER = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string APP_NAME = "OutfitTool";

        public void AddToStartup()
        {
            string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupManager.AUTORUN_REGISTRY_FOLDER, true))
            {
                if (key != null)
                {
                    key.SetValue(StartupManager.APP_NAME, "\"" + appPath + "\"");
                }
            }
        }

        public void RemoveFromStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupManager.AUTORUN_REGISTRY_FOLDER, true))
            {
                if (key != null)
                {
                    key.DeleteValue(StartupManager.APP_NAME, false);
                }
            }
        }
    }
}
