/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
namespace OutfitTool.Services.Settings;

public class AppSettings
{
    public const string DEFAULT_UPDATES_REPOSITORY = "https://outfit-tool.ru/repository.yaml";

    public List<string> EnabledModules { get; set; } = []; // setter is needed and is used in SettingsManager
    public bool MinimizeOnClose = true;
    public bool MinimizeOnStart = true;
    public bool LoadOnSystemStart = true;
    public bool CheckUpdatesOnStart = true;
    public string UpdatesRepository = "https://outfit-tool.ru/repository.yaml";
    public bool InstallUpdates = false;
    public Dictionary<string,string> HotKeys { get; set; } = new();  // setter is needed and is used in SettingsManager

    public string Language = "ru-RU";
}