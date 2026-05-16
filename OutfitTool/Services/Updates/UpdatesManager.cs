/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using System.Globalization;
using OutfitTool.ModulesAdapter.ModuleManager;
using OutfitTool.Services.Settings;
using OutfitTool.View;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using OutfitTool.ModulesAdapter.Dto;
using OutfitTool.ModulesAdapter.Logger;
using YamlDotNet.Serialization;

namespace OutfitTool.Services.Updates;

class UpdatesManager: IUpdatesManagerInterface
{
    public delegate void LoadListHandler(bool busy, RepositoryCollection? repositoryItems);
    public delegate void OnUpdateManagerDownloadChanged(string archivePath);
    private readonly ILogger _logger;
    private readonly FilesUpdater _filesUpdater;

    public UpdatesManager(FilesUpdater filesUpdater)
    {
        _filesUpdater = filesUpdater;
        _logger = ServiceLocator.GetService<ILogger>();
        _logger.Status("Обновления не проверялись");
    }

    public void DeleteModule(RepositoryItem module)
    {
        RestartApp(null, module);
    }

    public async void Download(RepositoryItem item)
    {
        string from = item.Url;
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updates");
        string to = Path.Combine(dir, item.Name + ".zip");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        if (File.Exists(to)) File.Delete(to);

        await DownloadModule(from, to);

        UnpackModule(item, to);
        RestartApp(item, null);
    }

    public async void UpdateAll()
    {
        // Получаем список установленных модулей
        var moduleManager = ServiceLocator.GetService<IModuleManager>();
        var existingModules = moduleManager.GetModules();

        // Получаем список обновлений
        var repositoryItems = await CheckForUpdates();

        // Чистим директорию updates
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updates");
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }
        Directory.CreateDirectory(dir);

        // Собираем последние версии модулей в репозитории
        var lastModules = repositoryItems?.GetLastVersionModules();
        var modulesToUpdate = new List<RepositoryItem>();
        foreach (var lastRepoModule in lastModules??[])
        {
            bool foundAndNeedUpdate = false;
            foreach (var existingModule in existingModules) 
            { 
                if (
                    existingModule.ModuleInfo.AssemblyName == lastRepoModule.AssemblyName 
                    && existingModule.ModuleInfo.Version < lastRepoModule.Version
                )
                {
                    foundAndNeedUpdate = true;
                    break;
                }
            }
            if (foundAndNeedUpdate)
            {
                modulesToUpdate.Add(lastRepoModule);
                _logger.Info($"Найден модуль для обновления: {lastRepoModule.AssemblyName}, версия {lastRepoModule.Version}");
            }
        }
        if (modulesToUpdate.Count > 0)
        {
            await DownloadModules(modulesToUpdate);
            RestartApp(null, null);
        }
    }

    private async Task DownloadModules(List<RepositoryItem> items)
    {
        var tasks = new List<Task<byte[]>>();
        foreach (var item in items)
        {
            var from = item.Url;
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updates");
            var to = Path.Combine(dir, item.Name + ".zip");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(to)) File.Delete(to);

            tasks.Add(DownloadModule(from, to));
        }
        _ = await Task.WhenAll(tasks);

        foreach (var item in items)
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updates");
            var to = Path.Combine(dir, item.Name + ".zip");
            UnpackModule(item, to);
        }
    }

    public async void LoadList(LoadListHandler updateManagerCheckStatusChanged)
    {
        try
        {
            updateManagerCheckStatusChanged(true, null);
            _logger.Status("Чтение списка обновлений..");

            var repositoryItems = await CheckForUpdates();
            _logger.Status("Прочитан список модулей из репозитория");

            updateManagerCheckStatusChanged(false, repositoryItems);
        }
        catch (Exception ex) {
            _logger.Status($"Ошибка при проверке обновлений: {ex.Message}");
            updateManagerCheckStatusChanged(false, null);
        }
    }

    private Dictionary<string, string> GetMainWindowParameters(string? selectedItemName = null)
    {
        var parameters = new Dictionary<string, string>
        {
            { "selected_tab", "1" },
            { "selected_item", selectedItemName ?? "null" },
            { "left", MainWindow.Instance?.Left.ToString(CultureInfo.InvariantCulture) ?? "null" },
            { "top", MainWindow.Instance?.Top.ToString(CultureInfo.InvariantCulture) ?? "null" },
            { "width", MainWindow.Instance?.Width.ToString(CultureInfo.InvariantCulture) ?? "null" },
            { "height", MainWindow.Instance?.Height.ToString(CultureInfo.InvariantCulture) ?? "null" }
        };

        return parameters;
    }


    private async Task<RepositoryCollection?> CheckForUpdates()
    {
        var settings = ServiceLocator.GetService<SettingsManager<AppSettings>>().LoadSettings();

        try
        {
            var client = new HttpClient();
            var response = await client.GetAsync(settings.UpdatesRepository);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return ParseUpdateList(content);
        }
        catch (Exception)
        {
            _logger.Error("Репозиторий " + settings.UpdatesRepository + " недоступен");
            return new RepositoryCollection();
        }
    }

    private RepositoryCollection ParseUpdateList(string yamlContent)
    {
        var deserializer = new DeserializerBuilder().Build();
        Dictionary<object, object> result = deserializer.Deserialize<dynamic>(yamlContent);

        if (!result.ContainsKey("url") || !result.ContainsKey("modules"))
        {
            throw new Exception("Invalid updates repository yaml format");
        }

        var repositoryUrl = result.GetValueOrDefault("url") as string;
        var modules = result.GetValueOrDefault("modules", new Dictionary<object, object>()) as Dictionary<object, object>;

        if (repositoryUrl == null || modules == null)
        {
            throw new Exception("Invalid updates repository yaml format");
        }

        var repositoryItems = new RepositoryCollection();

        foreach (var entry in modules)
        {
            if (entry.Value is not Dictionary<object, object> versionList)
            {
                throw new Exception("Invalid updates repository yaml format");
            }
            foreach (var versionEntry in versionList)
            {
                var version = versionEntry.Key.ToString();
                var parametersList = versionEntry.Value as Dictionary<object, object>;
                if (version == null || parametersList == null)
                {
                    continue;
                }

                RepositoryItem item = new RepositoryItem
                {
                    Version = ModuleVersion.FromString(version, GetValue(parametersList, "version_label")??"")
                };

                var name = entry.Key.ToString();
                if (name == null) continue;
                item.Name = name;

                item.Name = GetValue(parametersList, "name") ?? "";
                item.DisplayName = GetValue(parametersList, "display_name") ?? "";
                item.AssemblyName = GetValue(parametersList, "assembly_name") ?? "";
                item.Description = GetValue(parametersList, "description") ?? "";
                var requiredApi = GetValue(parametersList, "required_api") ?? "1";
                item.RequiredApi = int.Parse(requiredApi);
                item.Changes = GetValue(parametersList, "changes") ?? "";
                item.Author = GetValue(parametersList, "author") ?? "";
                item.AuthorContacts = GetValue(parametersList, "author_contacts") ?? "";

                item.Url = Path.Combine(
                    repositoryUrl, 
                    item.Name, 
                    item.Version.ToString(), 
                    "package.zip"
                ).Replace('\\','/');

                repositoryItems.Add(item);
            }
        }
        return repositoryItems;
    }

    private async Task<byte[]> DownloadModule(string from, string to)
    {
        var client = new HttpClient();
        var response = await client.GetByteArrayAsync(from);
        File.WriteAllBytes(to, response);
        return response;
    }

    private static void UnpackModule(RepositoryItem module, string downloadedArchive)
    {
        // Распаковываем модуль в /updates
        string extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updates", module.AssemblyName);
        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true);
        }
        ZipFile.ExtractToDirectory(downloadedArchive, extractPath);

        // Удаляем архив
        File.Delete(downloadedArchive);
    }

    private void RestartApp(RepositoryItem? selectedModule, RepositoryItem? moduleToDelete)
    {
        // Получаем параметры для восстановления состояния
        var mainAppParameters = GetMainWindowParameters(selectedModule?.Name);
    
        var modulesToDelete = new List<string>();
        if (moduleToDelete != null)
        {
            modulesToDelete.Add(moduleToDelete.AssemblyName);
        }
    
        _filesUpdater.Restart(modulesToDelete, mainAppParameters);
        Application.Current.Shutdown();
    }


    private static string? GetValue(Dictionary<object, object> parameters, string key)
    {
        string? value = null;
        if (parameters.ContainsKey(key) && parameters[key].ToString() != null)
        {
            value = parameters[key].ToString();
        }
        return value;
    }
}