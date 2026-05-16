/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.API1.Dto;
using OutfitTool.API1.Logger;
using System.Windows.Media.Imaging;

namespace OutfitTool.API1;

public interface IModuleController
{
    void SetLanguage(string language);
    void Init(ILogger logger);
    void Shutdown();
    List<ICommand> GetCommandList();
    BitmapImage? GetTaskbarIcon();
    string? GetTaskbarIconText();
    Notification? PopNotification();
    void OpenSettings();
}