/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.Services.HotkeyManager;

namespace OutfitTool.ModulesAdapter.ModuleManager;

interface IModuleManager
{
    List<Module> GetModules();
    public void LoadModules(List<string>? enabledModules = null);
    public Module? GetModule(string name);
    public CommandDescriptor? GetCommandDescriptor(string commandString);
}