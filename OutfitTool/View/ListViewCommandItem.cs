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

namespace OutfitTool.View;

public class ListViewCommandItem
{
    public ListViewCommandItem(ICommand command, Module module, string hotKey)
    {
        this.Command = command;
        this.Module = module;
        this.HotKey = hotKey;
    }

    public ICommand Command { get; set; }
    public Module Module { get; set; }
    public string HotKey { get; set; }
}