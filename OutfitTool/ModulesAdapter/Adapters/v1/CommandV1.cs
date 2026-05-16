/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.ModulesAdapter.Dto;

namespace OutfitTool.ModulesAdapter.Adapters.v1;

public class CommandV1: ICommand
{
    private OutfitTool.API1.ICommand _moduleCommand;
    public CommandV1(OutfitTool.API1.ICommand moduleCommand) 
    {
        this._moduleCommand = moduleCommand;
    }

    public string Name => this._moduleCommand.Name;

    public string Description => this._moduleCommand.Description;

    public CommandContextMenu? ContextMenu => _moduleCommand.ContextMenu != null ? new CommandContextMenu(
        _moduleCommand.ContextMenu.Text,
        _moduleCommand.ContextMenu.Image
    ):null;

    public void Run()
    {
        this._moduleCommand.Run();
    }
}