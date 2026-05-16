/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using static OutfitTool.Services.Updates.UpdatesManager;

namespace OutfitTool.Services.Updates;

interface IUpdatesManagerInterface
{
    public void LoadList(LoadListHandler updateManagerCheckStatusChanged);
    public void UpdateAll();
    public void Download(RepositoryItem item);
    public void DeleteModule(RepositoryItem module);
}