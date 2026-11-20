/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using OutfitTool.ModulesAdapter.Dto;
using OutfitTool.ModulesAdapter.Logger;
using OutfitTool.ModulesAdapter.ModuleManager;
using OutfitTool.Services.Settings;
using OutfitTool.View;
using YamlDotNet.Serialization;

namespace OutfitTool.Services.Updates;

class UpdatesManager : IUpdatesManagerInterface
{
    public delegate void LoadListHandler(bool busy, RepositoryCollection? repositoryItems);

    private const long MaxPackageSizeBytes = 200L * 1024 * 1024;

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    private static readonly Regex SafeNameRegex = new(
        @"^[A-Za-z0-9][A-Za-z0-9_.-]{0,127}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

    public async Task DownloadAsync(RepositoryItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            await DownloadInternal(item, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.Status("Загрузка обновления отменена.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка загрузки обновления {item.AssemblyName}: {ex}");
            MessageBox.Show(
                $"Не удалось скачать обновление: {ex.Message}",
                "Ошибка обновления",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public async Task UpdateAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await UpdateAllInternal(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.Status("Автоматическое обновление отменено.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка автоматического обновления: {ex}");
            MessageBox.Show(
                $"Не удалось выполнить автоматическое обновление: {ex.Message}",
                "Ошибка обновления",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task DownloadInternal(RepositoryItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRepositoryItem(item);

        var dir = GetUpdatesDirectory();
        Directory.CreateDirectory(dir);

        var archivePath = Path.Combine(dir, item.Name + ".zip");
        if (!IsPathUnder(dir, archivePath))
        {
            throw new InvalidOperationException("Недопустимый путь архива обновления.");
        }

        if (File.Exists(archivePath)) File.Delete(archivePath);

        var bytes = await DownloadModule(item.Url, archivePath, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        VerifyPackage(item, bytes);
        cancellationToken.ThrowIfCancellationRequested();
        UnpackModule(item, archivePath);
        cancellationToken.ThrowIfCancellationRequested();
        RestartApp(item, null);
    }

    private async Task UpdateAllInternal(CancellationToken cancellationToken)
    {
        var moduleManager = ServiceLocator.GetService<IModuleManager>();
        var existingModules = moduleManager.GetModules();

        var repositoryItems = await CheckForUpdates(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var dir = GetUpdatesDirectory();
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }

        Directory.CreateDirectory(dir);

        var lastModules = repositoryItems.GetLastVersionModules();
        var modulesToUpdate = new List<RepositoryItem>();

        foreach (var lastRepoModule in lastModules)
        {
            if (!IsApiCompatible(lastRepoModule))
            {
                _logger.Warning(
                    $"Пропущено обновление {lastRepoModule.AssemblyName}: требуется API {lastRepoModule.RequiredApi}, " +
                    $"поддерживается {string.Join(",", App.ModuleInfo.SupportedApiVersions)}.");
                continue;
            }

            var foundAndNeedUpdate = false;

            if (string.Equals(lastRepoModule.AssemblyName, App.ModuleInfo.AssemblyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                foundAndNeedUpdate = App.ModuleInfo.Version < lastRepoModule.Version;
            }
            else
            {
                foreach (var existingModule in existingModules)
                {
                    if (existingModule.ModuleInfo.AssemblyName == lastRepoModule.AssemblyName
                        && existingModule.ModuleInfo.Version < lastRepoModule.Version)
                    {
                        foundAndNeedUpdate = true;
                        break;
                    }
                }
            }

            if (!foundAndNeedUpdate) continue;

            modulesToUpdate.Add(lastRepoModule);
            _logger.Info($"Найден пакет для обновления: {lastRepoModule.AssemblyName}, версия {lastRepoModule.Version}");
        }

        if (modulesToUpdate.Count == 0) return;

        await DownloadModules(modulesToUpdate, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        RestartApp(null, null);
    }

    private async Task DownloadModules(List<RepositoryItem> items, CancellationToken cancellationToken)
    {
        var dir = GetUpdatesDirectory();
        Directory.CreateDirectory(dir);

        var tasks = new List<Task<byte[]>>();
        foreach (var item in items)
        {
            ValidateRepositoryItem(item);

            var to = Path.Combine(dir, item.Name + ".zip");
            if (!IsPathUnder(dir, to))
            {
                throw new InvalidOperationException("Недопустимый путь архива обновления.");
            }

            if (File.Exists(to)) File.Delete(to);
            tasks.Add(DownloadModule(item.Url, to, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);
        cancellationToken.ThrowIfCancellationRequested();

        for (var i = 0; i < items.Count; i++)
        {
            VerifyPackage(items[i], results[i]);
            UnpackModule(items[i], Path.Combine(dir, items[i].Name + ".zip"));
        }
    }

    public async Task LoadListAsync(LoadListHandler updateManagerCheckStatusChanged,
        CancellationToken cancellationToken = default)
    {
        try
        {
            updateManagerCheckStatusChanged(true, null);
            _logger.Status("Чтение списка обновлений..");

            var repositoryItems = await CheckForUpdates(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _logger.Status("Прочитан список модулей из репозитория");

            updateManagerCheckStatusChanged(false, repositoryItems);
        }
        catch (OperationCanceledException)
        {
            _logger.Status("Проверка обновлений отменена.");
            updateManagerCheckStatusChanged(false, null);
        }
        catch (Exception ex)
        {
            _logger.Status($"Ошибка при проверке обновлений: {ex.Message}");
            _logger.Error($"Ошибка при проверке обновлений: {ex}");
            updateManagerCheckStatusChanged(false, null);
        }
    }

    private Dictionary<string, string> GetMainWindowParameters(string? selectedItemName = null)
    {
        var parameters = new Dictionary<string, string>();

        if (MainWindow.Instance == null) return parameters;

        parameters["selected_tab"] = "1";
        if (!string.IsNullOrWhiteSpace(selectedItemName))
        {
            parameters["selected_item"] = selectedItemName;
        }

        parameters["left"] = MainWindow.Instance.Left.ToString(CultureInfo.InvariantCulture);
        parameters["top"] = MainWindow.Instance.Top.ToString(CultureInfo.InvariantCulture);
        parameters["width"] = MainWindow.Instance.Width.ToString(CultureInfo.InvariantCulture);
        parameters["height"] = MainWindow.Instance.Height.ToString(CultureInfo.InvariantCulture);

        return parameters;
    }

    private async Task<RepositoryCollection> CheckForUpdates(CancellationToken cancellationToken)
    {
        var settings = ServiceLocator.GetService<SettingsManager<AppSettings>>().LoadSettings();
        var repositoryUrl = settings.UpdatesRepository;
        var pinnedPublicKey = settings.UpdatesPublicKey;

        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps
                && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
        {
            _logger.Error($"Недопустимый адрес репозитория обновлений: '{repositoryUrl}'.");
            throw new InvalidOperationException("Укажите HTTPS-адрес репозитория обновлений.");
        }

        try
        {
            using var response = await HttpClient.GetAsync(uri, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseUpdateList(content, pinnedPublicKey);
        }
        catch (Exception ex)
        {
            _logger.Error($"Репозиторий {repositoryUrl} недоступен: {ex.Message}");
            throw;
        }
    }

    private RepositoryCollection ParseUpdateList(string yamlContent, string? pinnedPublicKey)
    {
        var deserializer = new DeserializerBuilder().Build();
        var result = deserializer.Deserialize<Dictionary<object, object>>(yamlContent);

        if (result == null
            || !result.TryGetValue("url", out var urlValue)
            || !result.TryGetValue("modules", out var modulesValue)
            || modulesValue is not Dictionary<object, object> modules)
        {
            throw new Exception("Invalid updates repository yaml format");
        }

        var repositoryUrl = urlValue?.ToString();
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var repositoryUri)
            || (repositoryUri.Scheme != Uri.UriSchemeHttps && !repositoryUri.IsLoopback))
        {
            throw new Exception("Invalid updates repository url");
        }

        var repositoryItems = new RepositoryCollection();

        var manifestPublicKey = result.TryGetValue("public_key", out var publicKeyValue)
            ? publicKeyValue?.ToString()
            : null;
        var effectivePublicKey = string.IsNullOrWhiteSpace(pinnedPublicKey)
            ? manifestPublicKey
            : pinnedPublicKey;

        if (!string.IsNullOrWhiteSpace(pinnedPublicKey)
            && !string.IsNullOrWhiteSpace(manifestPublicKey)
            && !string.Equals(pinnedPublicKey.Trim(), manifestPublicKey.Trim(), StringComparison.Ordinal))
        {
            _logger.Warning("Публичный ключ в манифесте отличается от закреплённого; используется закреплённый ключ.");
        }

        foreach (var entry in modules)
        {
            if (entry.Value is not Dictionary<object, object> versionList)
            {
                _logger.Warning("Пропущена запись репозитория: ожидался список версий.");
                continue;
            }

            var packageName = entry.Key?.ToString();
            if (string.IsNullOrWhiteSpace(packageName)) continue;

            foreach (var versionEntry in versionList)
            {
                try
                {
                    var versionString = versionEntry.Key?.ToString();
                    if (string.IsNullOrWhiteSpace(versionString)) continue;
                    if (versionEntry.Value is not Dictionary<object, object> parameters) continue;

                    var version = ModuleVersion.FromString(versionString, GetValue(parameters, "version_label") ?? "");

                    var item = new RepositoryItem
                    {
                        Version = version,
                        Name = GetSafeName(GetValue(parameters, "name"), packageName),
                        DisplayName = GetValue(parameters, "display_name") ?? "",
                        AssemblyName = GetSafeName(GetValue(parameters, "assembly_name"), packageName),
                        Description = GetValue(parameters, "description") ?? "",
                        Changes = GetValue(parameters, "changes") ?? "",
                        Author = GetValue(parameters, "author") ?? "",
                        AuthorContacts = GetValue(parameters, "author_contacts") ?? "",
                        Sha256 = GetValue(parameters, "sha256") ?? "",
                        Signature = GetValue(parameters, "signature") ?? "",
                        PublicKey = effectivePublicKey ?? ""
                    };

                    var requiredApi = GetValue(parameters, "required_api") ?? "1";
                    item.RequiredApi = int.TryParse(requiredApi, out var api) ? api : 1;

                    item.Url = BuildPackageUrl(repositoryUri, item.Name, item.Version);
                    repositoryItems.Add(item);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Пропущена версия '{versionEntry.Key}' пакета '{packageName}': {ex.Message}");
                }
            }
        }

        return repositoryItems;
    }

    private static string BuildPackageUrl(Uri repositoryUri, string packageName, ModuleVersion version)
    {
        var baseUrl = repositoryUri.ToString().TrimEnd('/');
        return $"{baseUrl}/{Uri.EscapeDataString(packageName)}/{Uri.EscapeDataString(version.ToString())}/package.zip";
    }

    private async Task<byte[]> DownloadModule(string from, string to, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(from, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback))
        {
            throw new InvalidOperationException($"Недопустимый URL пакета: '{from}'.");
        }

        using var response = await HttpClient.GetAsync(uri, cancellationToken);

        if (response.Content.Headers.ContentLength is > MaxPackageSizeBytes)
        {
            throw new InvalidOperationException("Пакет обновления слишком большой.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.LongLength > MaxPackageSizeBytes)
        {
            throw new InvalidOperationException("Пакет обновления слишком большой.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        File.WriteAllBytes(to, bytes);
        return bytes;
    }

    private static void UnpackModule(RepositoryItem module, string downloadedArchive)
    {
        var updatesDirectory = Path.GetFullPath(GetUpdatesDirectory());
        var extractPath = Path.GetFullPath(Path.Combine(updatesDirectory, module.AssemblyName));

        if (!IsPathUnder(updatesDirectory, extractPath))
        {
            throw new InvalidOperationException("Путь распаковки выходит за пределы каталога updates.");
        }

        using (var archive = ZipFile.OpenRead(downloadedArchive))
        {
            foreach (var entry in archive.Entries)
            {
                var entryPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
                if (!IsPathUnder(extractPath, entryPath))
                {
                    throw new InvalidOperationException(
                        $"Архив содержит недопустимый путь: '{entry.FullName}'.");
                }

                if (entry.Length > MaxPackageSizeBytes)
                {
                    throw new InvalidOperationException(
                        $"Файл '{entry.FullName}' в архиве слишком большой.");
                }
            }
        }

        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true);
        }

        ZipFile.ExtractToDirectory(downloadedArchive, extractPath);
        File.Delete(downloadedArchive);
    }

    private void RestartApp(RepositoryItem? selectedModule, RepositoryItem? moduleToDelete)
    {
        try
        {
            var mainAppParameters = GetMainWindowParameters(selectedModule?.Name);

            var modulesToDelete = new List<string>();
            if (moduleToDelete != null)
            {
                modulesToDelete.Add(moduleToDelete.AssemblyName);
            }

            _filesUpdater.Restart(modulesToDelete, mainAppParameters);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            _logger.Error($"Не удалось запустить обновление: {ex}");
            MessageBox.Show(
                $"Не удалось запустить обновление: {ex.Message}",
                "Ошибка обновления",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static void ValidateRepositoryItem(RepositoryItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (!SafeNameRegex.IsMatch(item.Name)) throw new InvalidOperationException($"Недопустимое имя пакета: '{item.Name}'.");
        if (!SafeNameRegex.IsMatch(item.AssemblyName)) throw new InvalidOperationException($"Недопустимое имя сборки: '{item.AssemblyName}'.");
        if (item.Version is null) throw new InvalidOperationException("Не задана версия пакета.");
    }

    private static string GetSafeName(string? value, string fallback)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        if (!SafeNameRegex.IsMatch(candidate))
        {
            throw new InvalidOperationException($"Недопустимое имя пакета/сборки: '{candidate}'.");
        }

        return candidate;
    }

    private static string? GetValue(Dictionary<object, object> parameters, string key)
    {
        return parameters.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static bool IsApiCompatible(RepositoryItem item)
    {
        return App.ModuleInfo.SupportedApiVersions.Contains(item.RequiredApi);
    }

    private static string GetUpdatesDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "updates");
    }

    private static bool IsPathUnder(string root, string path)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                             + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private void VerifyPackage(RepositoryItem item, byte[] bytes)
    {
        PackageVerifier.Verify(item, bytes, _logger);
    }
}
