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

    public static string[] GetModuleAndCommandNames(string name)
    {
        return name.Split('.');
    }
}