using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutfitTool.Services
{
    class StartParameters
    {
        private static Dictionary<string,string> parameters = new Dictionary<string, string>();
        private static List<string> allowedArguments = new List<string>{
            "left",
            "top",
            "width",
            "height",
            "selected_tab", // Число от 0 - номер открытой вкладки 
            "selected_item"
        };

        public static void Add(string key, string value)
        {
            parameters[key] = value;
        }

        public static T GetOne<T>(string key)
        {
            return (T)Convert.ChangeType(parameters[key], typeof(T));
        }

        public static bool ContainsKey(string key)
        {
            return parameters.ContainsKey(key);
        }

        public static void FillFromCommandLineArguments(System.Windows.StartupEventArgs e)
        {
            parameters = new Dictionary<string, string>();

            if (e.Args.Length > 0)
            {
                foreach (var arg in e.Args)
                {
                    var parts = arg.Split('=');
                    if (parts.Length == 2 && allowedArguments.Contains(parts[0]))
                    {
                        parameters.Add(parts[0], parts[1]);
                    }
                }
            }
        }

    }
}
