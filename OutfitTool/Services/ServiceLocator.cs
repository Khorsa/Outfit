/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.ModulesAdapter;
using OutfitTool.ModulesAdapter.Logger;
using OutfitTool.ModulesAdapter.ModuleManager;
using OutfitTool.Services.HotkeyManager;
using OutfitTool.Services.Settings;
using OutfitTool.Services.Updates;
using OutfitTool.View;

namespace OutfitTool.Services;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> Services = new();

    public static void RegisterServices()
    {
        Register<ILogger>(() => new OutfitTool.Logger.Logger());
        Register<IModuleManager>(() => new ModuleManager(GetService<ILogger>()));
        Register(() => new SettingsManager<AppSettings>());
        Register(() => new StartupManager());
        Register(() => new FilesUpdater(GetService<ILogger>()));
        Register<IUpdatesManagerInterface>(() => new UpdatesManager(GetService<FilesUpdater>()));
        Register(() => new RepositoryItemConverter());
        Register(() => new HotKeyManager(GetService<SettingsManager<AppSettings>>()));
    }
    public static T GetService<T>() where T : class
    {
        var service = TryToGetService<T>();
        if (service == null)
        {
            throw new InvalidOperationException($"Service of type {typeof(T)} not registered.");
        }
        return service;
    }

    private static void Register<T>(Func<T> factory) where T : class
    {
        if (TryToGetService<T>() == null)
        {
            Services[typeof(T)] = factory();
        }
    }

    private static T? TryToGetService<T>() where T : class
    {
        return Services.TryGetValue(typeof(T), out var service) ? service as T : null;
    }
}