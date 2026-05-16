/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.ModulesAdapter.Adapters;
using OutfitTool.Services.HotkeyManager;
using System.IO;
using System.Reflection;
using OutfitTool.ModulesAdapter.Logger;

namespace OutfitTool.ModulesAdapter.ModuleManager;

class ModuleManager(ILogger logger) : IModuleManager
{
    private const string MODULES_DIRECTORY = "modules";
    private readonly List<Module> _modules = [];

    public void LoadModules(List<string>? enabledModules = null)
    {
        enabledModules ??= [];

        // Останавливаем все модули
        foreach (var module in _modules)
        {
            module.ModuleController.Shutdown();
        }

        // Обнуляем список
        _modules.Clear();

        // Подгружаем список установленных модулей
        var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var modulesPath = Path.Combine(currentDirectory, MODULES_DIRECTORY);

        if (Directory.Exists(modulesPath))
        {
            var modulesDirs = Directory.GetDirectories(modulesPath);

            foreach (var moduleDir in modulesDirs)
            {
                try
                {
                    var moduleName = Path.GetFileName(moduleDir);
                    var dllFile = Path.Combine(moduleDir, moduleName + ".dll");
                    var assembly = Assembly.LoadFrom(dllFile);
                    var module = ParseModule(assembly);
                    
                    module.Enabled = enabledModules.Contains(module.AssemblyName);

                    _modules.Add(module);
                    logger.Info("Module " + module.AssemblyName + " loaded");
                }
                catch (Exception exception)
                {
                    logger.Debug(exception.ToString());
                }
            }
        }
        else
        {
            Directory.CreateDirectory(modulesPath);
        }
    }

    private static Module ParseModule(Assembly assembly)
    {
        var types = assembly.GetTypes();

        IModuleInfo? info = null;
        IModuleController? controller = null;

        foreach (var type in types)
        {
            info = new ModuleInfoAdapter(type).Get();
            if (info != null) { break; }
        }
        foreach (var type in types)
        {
            controller = new ModuleControllerAdapter(type).Get();
            if (controller != null) { break; }
        }

        var name = info?.Name;

        if (info == null)
        {
            throw new Exception("ModuleInfoInterface implementation not found in module " + name);
        }
        if (controller == null)
        {
            throw new Exception("ModuleControllerInterface implementation not found in module " + name);
        }
        if (name == null)
        {
            throw new Exception("Module name can't be resolved");
        }


        return new Module(name, info, controller);
    }

    public List<Module> GetModules()
    {
        return _modules;
    }
    public Module? GetModule(string name)
    {
        return _modules.FirstOrDefault(module => module.ModuleInfo.Name == name);
    }

    public CommandDescriptor? GetCommandDescriptor(string commandString)
    {
        var a = CommandDescriptor.GetModuleAndCommandNames(commandString);

        var module = GetModule(a[0]);
        ICommand? command = null;
        if (module != null)
        {
            foreach (var c in module.ModuleController.GetCommandList())
            {
                if (c.Name != a[1]) continue;
                command = c;
                break;
            }
        }

        if (module != null && command != null)
        {
            return new CommandDescriptor(module, command);
        }

        return null;
    }
}