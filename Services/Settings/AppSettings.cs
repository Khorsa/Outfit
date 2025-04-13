using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutfitTool.Services.Settings
{
    public class AppSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public readonly string defaultUpdatesRepository = "https://outfit-tool.ru/repository.yaml";

        public List<string> enabledModules { get; set; } = new List<string>();
        public bool minimizeOnClose = true;
        public bool loadOnSystemStart = true;
        public bool checkUpdatesOnStart = true;
        public string updatesRepository = "https://outfit-tool.ru/repository.yaml";
        public bool installUpdates = true;
        public bool installOnlyMinorUpdates = true;
        public Dictionary<string,string> hotKeys { get; set; } = new Dictionary<string, string>();

        public string language = "ru-RU";
    }
}
