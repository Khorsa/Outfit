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

namespace OutfitTool.ModulesAdapter;

public interface IModuleController
{
    public void SetLanguage(string language);
    public void Init(ILogger logger);
    public void Shutdown();
    List<ICommand> GetCommandList();
    public BitmapImage? GetTaskbarIcon();
    public string? GetTaskbarIconText();
    public Notification? PopNotification();
    public void OpenSettings();
}