/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
namespace OutfitTool.ModulesAdapter.ModuleManager;

/// <summary>
/// Нормализация списка включённых модулей.
/// В старых settings.yaml хранился ModuleInfo.Name, в новых — AssemblyName;
/// поддерживаем оба варианта, но записываем только AssemblyName.
/// </summary>
public static class ModuleEnablement
{
    public static void SetEnabled(
        List<string> enabledModules,
        string assemblyName,
        string moduleName,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(enabledModules);

        enabledModules.RemoveAll(existing =>
            string.Equals(existing, assemblyName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(existing, moduleName, StringComparison.OrdinalIgnoreCase));

        if (enabled && !string.IsNullOrWhiteSpace(assemblyName))
        {
            enabledModules.Add(assemblyName);
        }
    }

    public static bool IsEnabled(
        IEnumerable<string> enabledModules,
        string assemblyName,
        string moduleName)
    {
        return enabledModules.Any(existing =>
            string.Equals(existing, assemblyName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(existing, moduleName, StringComparison.OrdinalIgnoreCase));
    }
}
