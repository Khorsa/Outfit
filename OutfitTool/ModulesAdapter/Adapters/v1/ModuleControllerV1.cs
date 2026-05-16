/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.Windows.Media.Imaging;
using OutfitTool.ModulesAdapter.Dto;
using OutfitTool.ModulesAdapter.Logger;

namespace OutfitTool.ModulesAdapter.Adapters.v1;

public class ModuleControllerV1 : IModuleController
{
    OutfitTool.API1.IModuleController _api;

    public ModuleControllerV1(OutfitTool.API1.IModuleController api)
    {
        this._api = api;
    }

    public List<ICommand> GetCommandList()
    {
        List<OutfitTool.API1.ICommand> apiCommands = this._api.GetCommandList();
        List<ICommand> commands = new List<ICommand>();
        foreach(var cmd in apiCommands)
        {
            commands.Add(new CommandV1(cmd));
        }
        return commands;
    }

    public BitmapImage? GetTaskbarIcon()
    {
        return this._api.GetTaskbarIcon();
    }

    public string? GetTaskbarIconText()
    {
        return this._api.GetTaskbarIconText();
    }

    public void Init(ILogger logger)
    {
        this._api.Init(new LoggerV1(logger));
    }

    public void OpenSettings()
    {
        this._api.OpenSettings();
    }

    public Notification? PopNotification()
    {
        var apiNotification = _api.PopNotification();
        Notification? notification = null;
        if (apiNotification != null)
        {
            notification = new Notification(
                apiNotification.Title,
                apiNotification.Message,
                apiNotification.Href
            );
        }
        return notification;
    }

    public void SetLanguage(string language)
    {
        this._api.SetLanguage(language);
    }

    public void Shutdown()
    {
        this._api.Shutdown();
    }
}