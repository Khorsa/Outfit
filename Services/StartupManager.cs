using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OutfitTool.Services
{
    class StartupManager
    {
        public void AddToStartup()
        {
            string appName = "OutfitTool";
            string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {
                    key.SetValue(appName, "\"" + appPath + "\"");
                }
            }
        }

        public void RemoveFromStartup()
        {
            string appName = "OutfitTool";

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {
                    key.DeleteValue(appName, false);
                }
            }
        }
    }
}
