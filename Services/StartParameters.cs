namespace OutfitTool.Services;

static class StartParameters
{
    private static Dictionary<string,string> _parameters = new();
    private static readonly List<string> AllowedArguments =
    [
        "left",
        "top",
        "width",
        "height",
        "selected_tab", // Число от 0 - номер открытой вкладки 
        "selected_item"
    ];

    public static void Add(string key, string value)
    {
        _parameters[key] = value;
    }

    public static T GetOne<T>(string key)
    {
        return (T)Convert.ChangeType(_parameters[key], typeof(T));
    }

    public static bool ContainsKey(string key)
    {
        return _parameters.ContainsKey(key);
    }

    public static void FillFromCommandLineArguments(System.Windows.StartupEventArgs e)
    {
        _parameters = new Dictionary<string, string>();

        if (e.Args.Length > 0)
        {
            foreach (var arg in e.Args)
            {
                var parts = arg.Split('=');
                if (parts.Length == 2 && AllowedArguments.Contains(parts[0]))
                {
                    _parameters.Add(parts[0], parts[1]);
                }
            }
        }
    }

}