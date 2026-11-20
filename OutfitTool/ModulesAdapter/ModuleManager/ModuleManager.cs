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
    private readonly List<string> _loadErrors = [];

    public IReadOnlyList<string> LoadErrors => _loadErrors;

    public void LoadModules(List<string>? enabledModules = null)
    {
        enabledModules ??= [];
        var enabledSet = new HashSet<string>(enabledModules, StringComparer.OrdinalIgnoreCase);

        // Останавливаем и выгружаем все модули, включая статические окна/таймеры.
        foreach (var module in _modules)
        {
            module.Unload();
        }

        _modules.Clear();
        _loadErrors.Clear();

        var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var modulesPath = Path.Combine(currentDirectory, MODULES_DIRECTORY);

        if (!Directory.Exists(modulesPath))
        {
            Directory.CreateDirectory(modulesPath);
            return;
        }

        foreach (var moduleDir in Directory.GetDirectories(modulesPath))
        {
            ModuleLoadContext? context = null;
            var moduleName = Path.GetFileName(moduleDir);

            try
            {
                var dllFile = Path.Combine(moduleDir, moduleName + ".dll");
                if (!File.Exists(dllFile))
                {
                    throw new FileNotFoundException($"Не найдена сборка модуля '{moduleName}.dll'.", dllFile);
                }

                context = new ModuleLoadContext(dllFile);
                var assembly = context.LoadFromAssemblyPath(dllFile);

                var info = ParseModuleInfo(assembly);
                if (!App.ModuleInfo.SupportedApiVersions.Contains(info.RequireApiVersion))
                {
                    throw new NotSupportedException(
                        $"Модуль требует API {info.RequireApiVersion}, поддерживается " +
                        $"API {string.Join(",", App.ModuleInfo.SupportedApiVersions)}.");
                }

                var enabled = enabledSet.Contains(info.AssemblyName) || enabledSet.Contains(info.Name);
                var controller = enabled ? ParseModuleController(assembly) : new DisabledModuleController();

                var module = new Module(info.AssemblyName, info, controller, context)
                {
                    Enabled = enabled
                };

                _modules.Add(module);
                logger.Info($"Module {module.AssemblyName} loaded ({(enabled ? "enabled" : "disabled")})");
            }
            catch (Exception exception)
            {
                var message = $"Модуль '{moduleName}' не загружен: {exception.Message}";
                logger.Error(message);
                _loadErrors.Add(message);

                try { context?.Unload(); } catch { /* ignored */ }
            }
        }
    }

    private static IModuleInfo ParseModuleInfo(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract) continue;

            try
            {
                var info = new ModuleInfoAdapter(type).Get();
                if (info != null) return info;
            }
            catch
            {
                // Тип реализует интерфейс, но не может быть создан; пробуем следующий.
            }
        }

        throw new Exception("ModuleInfoInterface implementation not found in module.");
    }

    private static IModuleController ParseModuleController(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract) continue;

            try
            {
                var controller = new ModuleControllerAdapter(type).Get();
                if (controller != null) return controller;
            }
            catch
            {
                // Пробуем следующий тип; итоговая ошибка будет понятна вызывающему.
            }
        }

        throw new Exception("ModuleControllerInterface implementation not found in module.");
    }

    public List<Module> GetModules()
    {
        return _modules;
    }

    public Module? GetModule(string name)
    {
        return _modules.FirstOrDefault(module =>
            string.Equals(module.ModuleInfo.Name, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(module.AssemblyName, name, StringComparison.OrdinalIgnoreCase));
    }

    public CommandDescriptor? GetCommandDescriptor(string commandString)
    {
        var parts = CommandDescriptor.GetModuleAndCommandNames(commandString);
        if (parts.Length != 2)
        {
            logger.Warning($"Некорректная строка команды: '{commandString}'.");
            return null;
        }

        var module = GetModule(parts[0]);
        ICommand? command = null;
        if (module != null)
        {
            try
            {
                foreach (var c in module.ModuleController.GetCommandList())
                {
                    if (c.Name != parts[1]) continue;
                    command = c;
                    break;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка получения команд модуля {module.ModuleInfo.Name}: {ex.Message}");
                return null;
            }
        }

        return module != null && command != null
            ? new CommandDescriptor(module, command)
            : null;
    }
}
