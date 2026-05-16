/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
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
        "selected_item",
        "update_mode" // В режиме обновления заменяет файлы новыми и перезапускает программу
    ];

    public static List<string> ModulesToDelete = [];
    
    private static void Add(string key, string value)
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
        
        Add("update_mode", e.Args.Contains("--update").ToString());
        
        if (e.Args.Length > 0)
        {
            foreach (var arg in e.Args)
            {
                var parts = arg.Split('=');
                if (parts.Length == 2 && AllowedArguments.Contains(parts[0]))
                {
                    Add(parts[0], parts[1]);
                }
                else
                {
                    if (parts[0] == "delete_module")
                    {
                        ModulesToDelete.Add(parts[1]);
                    }
                    if (parts[0] == "delete_module")
                    {
                        ModulesToDelete.Add(parts[1]);
                    }
                }
            }
        }
    }

    public static Dictionary<string, string> GetAll()
    {
        return _parameters.Where(x => x.Key != "update_mode").ToDictionary();
    }

}