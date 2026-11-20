/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.IO;
using System.Text.RegularExpressions;

namespace OutfitTool.Services;

static class StartParameters
{
    private static Dictionary<string, string> _parameters = new();

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

    private static readonly Regex SafeModuleName = new(
        @"^[A-Za-z0-9][A-Za-z0-9_.-]{0,127}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static List<string> ModulesToDelete { get; } = [];

    /// <summary>
    /// Каталог установки, переданный обновлятором. null — обычный запуск из каталога приложения.
    /// </summary>
    public static string? InstallDirectory { get; private set; }

    private static void Add(string key, string value)
    {
        _parameters[key] = value;
    }

    public static T GetOne<T>(string key)
    {
        if (!_parameters.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"Параметр запуска '{key}' не найден.");
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Не удалось преобразовать параметр '{key}' = '{value}'.", ex);
        }
    }

    public static bool TryGetOne<T>(string key, out T value)
    {
        value = default!;
        if (!_parameters.TryGetValue(key, out var raw)) return false;

        try
        {
            value = (T)Convert.ChangeType(raw, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool ContainsKey(string key)
    {
        return _parameters.ContainsKey(key);
    }

    public static void FillFromCommandLineArguments(System.Windows.StartupEventArgs e)
    {
        _parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ModulesToDelete.Clear();
        InstallDirectory = null;

        Add("update_mode", e.Args.Any(arg =>
            string.Equals(arg, "--update", StringComparison.OrdinalIgnoreCase)).ToString());

        for (var i = 0; i < e.Args.Length; i++)
        {
            var arg = e.Args[i];

            if (arg.StartsWith("--install-dir=", StringComparison.OrdinalIgnoreCase))
            {
                SetInstallDirectory(arg["--install-dir=".Length..]);
                continue;
            }

            if (string.Equals(arg, "--install-dir", StringComparison.OrdinalIgnoreCase)
                && i + 1 < e.Args.Length)
            {
                SetInstallDirectory(e.Args[++i]);
                continue;
            }

            var separator = arg.IndexOf('=');
            if (separator <= 0) continue;

            var key = arg[..separator];
            var value = arg[(separator + 1)..];

            if (string.Equals(key, "delete_module", StringComparison.OrdinalIgnoreCase))
            {
                if (SafeModuleName.IsMatch(value) && !ModulesToDelete.Contains(value))
                {
                    ModulesToDelete.Add(value);
                }

                continue;
            }

            if (AllowedArguments.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                Add(key, value);
            }
        }
    }

    public static Dictionary<string, string> GetAll()
    {
        return _parameters
            .Where(x => !string.Equals(x.Key, "update_mode", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static void SetInstallDirectory(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        try
        {
            var normalized = Path.GetFullPath(value.Trim().Trim('"'));
            if (Directory.Exists(normalized))
            {
                InstallDirectory = normalized;
            }
        }
        catch
        {
            // Некорректный путь игнорируем; обновление не начнётся без валидного install-dir.
        }
    }
}
