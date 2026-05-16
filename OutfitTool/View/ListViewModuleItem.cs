/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.ModulesAdapter.ModuleManager;
using OutfitTool.Services;

namespace OutfitTool.View;

public class ListViewModuleItem
{
    public ListViewModuleItem(Module module)
    {
        this.Module = module;
        this.Name = module.ModuleInfo.Name;
        this.State = module.Enabled ? LocalizationHelper.GetString("On") : LocalizationHelper.GetString("Off");
    }

    public Module Module { get; set; }
    public string Name { get; set; }
    public string State { get; set; }
}