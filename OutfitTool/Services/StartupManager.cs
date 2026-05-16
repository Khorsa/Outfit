/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using Microsoft.Win32;

namespace OutfitTool.Services;

class StartupManager
{
    private const string AUTORUN_REGISTRY_FOLDER = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string APP_NAME = "OutfitTool";

    public void AddToStartup()
    {
        var appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

        using var key = Registry.CurrentUser.OpenSubKey(StartupManager.AUTORUN_REGISTRY_FOLDER, true);
        key?.SetValue(StartupManager.APP_NAME, "\"" + appPath + "\"");
    }

    public void RemoveFromStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupManager.AUTORUN_REGISTRY_FOLDER, true);
        key?.DeleteValue(StartupManager.APP_NAME, false);
    }
}