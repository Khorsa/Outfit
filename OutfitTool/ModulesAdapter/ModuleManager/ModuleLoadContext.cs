/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using OutfitTool.ModulesAdapter.Dto;
using OutfitTool.ModulesAdapter.Logger;

namespace OutfitTool.ModulesAdapter.ModuleManager;

/// <summary>
/// Collectible-контекст на модуль: позволяет выгрузить сборку при выключении/обновлении.
/// </summary>
internal sealed class ModuleLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver? _resolver;

    public ModuleLoadContext(string mainAssemblyPath)
        : base("OutfitTool.Module." + Path.GetFileNameWithoutExtension(mainAssemblyPath), isCollectible: true)
    {
        try
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
        }
        catch
        {
            _resolver = null;
        }
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // API1 должен быть общим для хоста и модуля, иначе типы IModuleController
        // из разных контекстов не совпадут и модуль не загрузится.
        if (string.Equals(assemblyName.Name, "API1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(assemblyName.Name, "OutfitTool.API1", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var path = _resolver?.ResolveAssemblyToPath(assemblyName);
            if (path != null)
            {
                return LoadFromAssemblyPath(path);
            }
        }
        catch
        {
            // Падение загрузки зависимости обрабатывается выше, в ModuleManager.
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        try
        {
            var path = _resolver?.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (path != null)
            {
                return LoadUnmanagedDllFromPath(path);
            }
        }
        catch
        {
            // Загрузка native-зависимости не должна ронять весь хост.
        }

        return IntPtr.Zero;
    }
}

/// <summary>
/// Заглушка для выключенного модуля: позволяет показать его в списке, не создавая окна/таймеры.
/// </summary>
internal sealed class DisabledModuleController : IModuleController
{
    public void SetLanguage(string language) { }
    public void Init(ILogger logger) { }
    public void Shutdown() { }
    public List<ICommand> GetCommandList() => [];
    public System.Windows.Media.Imaging.BitmapImage? GetTaskbarIcon() => null;
    public string? GetTaskbarIconText() => null;
    public Notification? PopNotification() => null;
    public void OpenSettings() { }
}
