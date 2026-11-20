/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.ModulesAdapter;
using OutfitTool.ModulesAdapter.ModuleManager;

namespace OutfitTool.Services.HotkeyManager;

public class CommandDescriptor
{
    public Module Module;
    public ICommand Command;

    public CommandDescriptor(
        Module module,
        ICommand command
    )
    {
        this.Module = module;
        this.Command = command;
    }

    public override string ToString()
    {
        return Module.AssemblyName + "." + Command.Name;
    }

    /// <summary>
    /// Сравнивает строку команды с учётом legacy-имени модуля (ModuleInfo.Name)
    /// и текущего AssemblyName.
    /// </summary>
    public bool Matches(string? commandString)
    {
        if (string.IsNullOrWhiteSpace(commandString)) return false;

        var parts = GetModuleAndCommandNames(commandString);
        if (parts.Length != 2) return false;

        var moduleMatches =
            string.Equals(parts[0], Module.AssemblyName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(parts[0], Module.ModuleInfo.Name, StringComparison.OrdinalIgnoreCase);

        return moduleMatches
               && string.Equals(parts[1], Command.Name, StringComparison.OrdinalIgnoreCase);
    }

    public static string[] GetModuleAndCommandNames(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return [];

        var separator = name.IndexOf('.');
        if (separator <= 0 || separator == name.Length - 1) return [];

        return [name[..separator], name[(separator + 1)..]];
    }
}