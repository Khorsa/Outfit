using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using OutfitTool.ModulesAdapter.Logger;

namespace OutfitTool.Services.Updates;

/// <summary>
/// Самообновление приложения.
///
/// Инварианты:
///  - апдейтер всегда работает с явно переданным каталогом установки, а не с BaseDirectory;
///  - новый файл-набор сначала собирается в staging и проверяется;
///  - текущая установка переименовывается в backup, только затем staging становится установкой;
///  - при любой ошибке выполняется попытка отката, процесс не завершается «в никуда».
/// </summary>
public class FilesUpdater
{
    private const string MainProcessName = "OutfitTool";
    private const int WaitForExitTimeoutSeconds = 30;
    private const int RetryDelayMs = 200;
    private const int MaxRetryAttempts = 150;

    private readonly ILogger _logger;
    private readonly string _installDirectory;
    private readonly string _cacheDirectory;
    private readonly string _stagingRoot;
    private readonly string _backupRoot;
    private readonly string _updatesDirectory;
    private readonly string _modulesDirectory;
    private readonly string _mainExePath;

    private static readonly Regex SafeNameRegex = new(
        @"^[A-Za-z0-9][A-Za-z0-9_.-]{0,127}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private string? _lastMainBackup;

    public FilesUpdater(ILogger logger, string? installDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _installDirectory = NormalizeDirectory(installDirectory ?? AppContext.BaseDirectory);
        _updatesDirectory = Path.Combine(_installDirectory, "updates");
        _modulesDirectory = Path.Combine(_installDirectory, "modules");
        _mainExePath = Path.Combine(_installDirectory, "OutfitTool.exe");
        _cacheDirectory = Path.Combine(Path.GetTempPath(), "OutfitTool", "update_cache");

        var parent = Directory.GetParent(_installDirectory)?.FullName
                     ?? Path.GetTempPath();
        _stagingRoot = Path.Combine(parent, ".outfittool-staging");
        _backupRoot = Path.Combine(parent, ".outfittool-backup");
    }

    /// <summary>
    /// Запускает копию обновлятора и завершает текущий процесс.
    /// </summary>
    public void Restart(List<string> modulesToDelete, Dictionary<string, string>? mainAppParameters = null)
    {
        _logger.Info("=== Начало процесса обновления ===");

        try
        {
            PrepareBootstrap();

            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(_cacheDirectory, "OutfitTool.exe"),
                WorkingDirectory = _cacheDirectory,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in BuildUpdateArguments(modulesToDelete, mainAppParameters))
            {
                startInfo.ArgumentList.Add(argument);
            }

            _logger.Info($"Запуск копии обновлятора: {startInfo.FileName} {string.Join(" ", startInfo.ArgumentList)}");
            var process = Process.Start(startInfo)
                          ?? throw new InvalidOperationException("Не удалось запустить копию обновлятора.");

            // Даём копии время стартовать. Если она сразу упала — не убиваем текущий процесс.
            Thread.Sleep(1500);
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Копия обновлятора завершилась с кодом {process.ExitCode}. Обновление отменено.");
            }

            _logger.Info("Копия обновлятора запущена, завершение текущего экземпляра.");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка при подготовке к обновлению: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Выполняет обновление из копии, запущенной с --update --install-dir=...
    /// </summary>
    /// <returns>true — обновление применено и новая версия запущена; false — ошибка, нужен откат/сообщение.</returns>
    public bool Update(List<string> modulesToDelete, Dictionary<string, string>? mainAppParameters = null)
    {
        _logger.Info("=== Запущен режим обновления ===");

        try
        {
            if (!WaitForMainProgramExit())
            {
                throw new TimeoutException("Основная программа не завершилась за отведённое время.");
            }

            if (!DeleteModules(modulesToDelete))
            {
                throw new IOException("Не удалось удалить один или несколько модулей.");
            }

            if (!ProcessUpdatesDirectory())
            {
                throw new IOException("Не удалось применить обновления.");
            }

            if (!RestartMainProgram(mainAppParameters))
            {
                throw new InvalidOperationException("Не удалось запустить обновлённую программу.");
            }

            _logger.Info("=== Обновление успешно завершено ===");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка в режиме обновления: {ex}");

            // Если основной модуль уже был заменён, но новая версия не стартовала — возвращаем backup.
            if (_lastMainBackup != null && Directory.Exists(_lastMainBackup))
            {
                _logger.Warning("Попытка отката основной программы из backup...");
                if (RollbackMainModule())
                {
                    _logger.Info("Откат основной программы выполнен.");
                }
                else
                {
                    _logger.Error("Откат основной программы не удался.");
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Удаляет временные каталоги обновления. Вызывается при обычном старте приложения.
    /// </summary>
    public void CleanupCache()
    {
        var updateInProgress = IsAnyProcessRunningFrom(_cacheDirectory);

        // Нельзя удалять каталог, из которого сейчас работает процесс.
        if (!updateInProgress
            && !PathsEqual(AppContext.BaseDirectory, _cacheDirectory))
        {
            TryDeleteDirectory(_cacheDirectory);
        }

        // Идёт обновление из кэша — не трогаем staging/backup, они ещё нужны.
        if (updateInProgress) return;

        // Совместимость со старыми версиями: кэш лежал внутри каталога установки.
        var legacyCache = Path.Combine(_installDirectory, "update_cache");
        if (!IsAnyProcessRunningFrom(legacyCache))
        {
            TryDeleteDirectory(legacyCache);
        }

        if (IsAnyProcessRunningFrom(_stagingRoot) || IsAnyProcessRunningFrom(_backupRoot))
        {
            return;
        }

        CleanupOldDirectories(_stagingRoot, TimeSpan.FromDays(2));
        CleanupOldDirectories(_backupRoot, TimeSpan.FromDays(30), keepNewest: true);
    }

    private static bool IsAnyProcessRunningFrom(string directory)
    {
        if (!Directory.Exists(directory)) return false;

        foreach (var process in Process.GetProcessesByName(MainProcessName))
        {
            try
            {
                var path = process.MainModule?.FileName;
                if (path != null && IsPathUnder(directory, path))
                {
                    return true;
                }
            }
            catch
            {
                // Не можем прочитать путь — не удаляем каталог на всякий случай.
                return true;
            }
            finally
            {
                process.Dispose();
            }
        }

        return false;
    }

    #region Подготовка bootstrap-копии

    private void PrepareBootstrap()
    {
        if (PathsEqual(_installDirectory, _cacheDirectory))
        {
            throw new InvalidOperationException(
                "Каталог установки совпадает с временным каталогом обновления. Проверьте аргумент --install-dir.");
        }

        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, true);
        }

        Directory.CreateDirectory(_cacheDirectory);

        var excludedAtRoot = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "modules",
            "updates",
            "logs",
            "settings.yaml",
            "update_cache",
            ".outfittool-staging",
            ".outfittool-backup"
        };

        CopyDirectory(_installDirectory, _cacheDirectory, excludedAtRoot);

        foreach (var required in new[]
                 {
                     "OutfitTool.exe",
                     "OutfitTool.dll",
                     "OutfitTool.runtimeconfig.json",
                     "OutfitTool.deps.json"
                 })
        {
            var path = Path.Combine(_cacheDirectory, required);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"В каталоге установки отсутствует обязательный файл '{required}'.", path);
            }
        }

        _logger.Info($"Bootstrap-копия подготовлена: {_cacheDirectory}");
    }

    private List<string> BuildUpdateArguments(
        List<string> modulesToDelete,
        Dictionary<string, string>? mainAppParameters)
    {
        var arguments = new List<string>
        {
            "--update",
            $"--install-dir={_installDirectory}"
        };

        foreach (var module in modulesToDelete)
        {
            if (!IsSafeName(module))
            {
                _logger.Warning($"Пропущено недопустимое имя модуля для удаления: '{module}'.");
                continue;
            }

            arguments.Add($"delete_module={module}");
        }

        if (mainAppParameters != null)
        {
            foreach (var (key, value) in mainAppParameters)
            {
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;
                if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)) continue;
                arguments.Add($"{key}={value}");
            }
        }

        return arguments;
    }

    #endregion

    #region Ожидание и удаление

    private bool WaitForMainProgramExit()
    {
        _logger.Info($"Ожидание завершения процесса {MainProcessName}...");

        var deadline = DateTime.UtcNow.AddSeconds(WaitForExitTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var process = FindOtherMainProcess();
            if (process == null)
            {
                _logger.Info("Основная программа не запущена.");
                Thread.Sleep(1000);
                return true;
            }

            try
            {
                if (process.HasExited)
                {
                    _logger.Info("Основная программа завершилась.");
                    Thread.Sleep(1000);
                    return true;
                }
            }
            catch
            {
                // Процесс уже завершился/недоступен.
                return true;
            }
            finally
            {
                process.Dispose();
            }

            Thread.Sleep(RetryDelayMs);
        }

        _logger.Error($"Таймаут ожидания завершения основной программы ({WaitForExitTimeoutSeconds} сек).");
        return false;
    }

    private Process? FindOtherMainProcess()
    {
        foreach (var process in Process.GetProcessesByName(MainProcessName))
        {
            if (process.Id == Environment.ProcessId)
            {
                process.Dispose();
                continue;
            }

            try
            {
                var fileName = process.MainModule?.FileName;
                if (fileName != null && PathsEqual(fileName, _mainExePath))
                {
                    return process;
                }
            }
            catch
            {
                // Не смогли определить путь (права/битый процесс) — считаем его нашей установкой.
                return process;
            }

            process.Dispose();
        }

        return null;
    }

    private bool DeleteModules(List<string> modulesToDelete)
    {
        foreach (var module in modulesToDelete)
        {
            if (!IsSafeName(module))
            {
                _logger.Error($"Недопустимое имя модуля для удаления: '{module}'.");
                return false;
            }

            var moduleDir = Path.Combine(_modulesDirectory, module);
            if (!IsPathUnder(_modulesDirectory, moduleDir))
            {
                _logger.Error($"Путь модуля выходит за пределы каталога modules: '{moduleDir}'.");
                return false;
            }

            if (!Directory.Exists(moduleDir))
            {
                _logger.Warning($"Модуль не найден: {moduleDir}");
                continue;
            }

            _logger.Info($"Удаление модуля: {module}...");
            if (!TryDeleteDirectory(moduleDir, MaxRetryAttempts))
            {
                _logger.Error($"Не удалось удалить модуль {module} после {MaxRetryAttempts} попыток.");
                return false;
            }

            _logger.Info($"Модуль удалён: {module}");
        }

        return true;
    }

    #endregion

    #region Обработка updates

    private bool ProcessUpdatesDirectory()
    {
        if (!Directory.Exists(_updatesDirectory))
        {
            _logger.Info("Папка updates не существует, обновления не требуются.");
            return true;
        }

        var subDirs = Directory.GetDirectories(_updatesDirectory);
        _logger.Info($"Найдено {subDirs.Length} папок обновлений.");

        var mainDirs = new List<string>();
        var moduleDirs = new List<string>();

        foreach (var dir in subDirs)
        {
            var dirName = Path.GetFileName(dir);
            if (string.Equals(dirName, MainProcessName, StringComparison.OrdinalIgnoreCase))
            {
                mainDirs.Add(dir);
            }
            else
            {
                moduleDirs.Add(dir);
            }
        }

        // Сначала модули, затем основной модуль: после замены каталога установки
        // пути к исходным папкам обновлений уже недействительны.
        foreach (var dir in moduleDirs)
        {
            var moduleName = Path.GetFileName(dir);
            if (!UpdateAdditionalModule(dir, moduleName)) return false;
            TryDeleteDirectory(dir);
        }

        foreach (var dir in mainDirs)
        {
            if (!UpdateMainModule(dir)) return false;
        }

        return true;
    }

    private bool UpdateAdditionalModule(string sourceDir, string moduleName)
    {
        if (!IsSafeName(moduleName))
        {
            _logger.Error($"Недопустимое имя модуля в updates: '{moduleName}'.");
            return false;
        }

        var destination = Path.Combine(_modulesDirectory, moduleName);
        if (!IsPathUnder(_modulesDirectory, destination))
        {
            _logger.Error($"Путь модуля выходит за пределы modules: '{destination}'.");
            return false;
        }

        var staging = Path.Combine(_stagingRoot, $"module-{moduleName}-{Guid.NewGuid():N}");
        try
        {
            _logger.Info($"Подготовка модуля {moduleName}...");
            CopyDirectory(sourceDir, staging);

            var expectedDll = Path.Combine(staging, moduleName + ".dll");
            if (!File.Exists(expectedDll))
            {
                _logger.Error($"В пакете модуля {moduleName} отсутствует {moduleName}.dll.");
                return false;
            }

            Directory.CreateDirectory(_modulesDirectory);
            string? backup = null;

            if (Directory.Exists(destination))
            {
                backup = Path.Combine(_backupRoot, $"module-{moduleName}-{DateTime.UtcNow:yyyyMMddHHmmss}");
                Directory.CreateDirectory(_backupRoot);
                MoveDirectory(destination, backup);
            }

            try
            {
                MoveDirectory(staging, destination);
                _logger.Info($"Модуль обновлён: {moduleName}");
                return true;
            }
            catch
            {
                if (backup != null && Directory.Exists(backup) && !Directory.Exists(destination))
                {
                    TryMoveDirectory(backup, destination);
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка обновления модуля {moduleName}: {ex.Message}");
            return false;
        }
        finally
        {
            TryDeleteDirectory(staging);
        }
    }

    private bool UpdateMainModule(string sourceDir)
    {
        var staging = Path.Combine(_stagingRoot, $"install-{Guid.NewGuid():N}");
        try
        {
            _logger.Info("Подготовка основной программы...");
            CopyDirectory(sourceDir, staging);
            CopyProtectedItems(_installDirectory, staging);
            ValidateInstallDirectory(staging);

            var backup = Path.Combine(_backupRoot, $"install-{DateTime.UtcNow:yyyyMMddHHmmss}");
            Directory.CreateDirectory(_backupRoot);
            Directory.CreateDirectory(_stagingRoot);

            MoveDirectory(_installDirectory, backup);
            _lastMainBackup = backup;

            try
            {
                MoveDirectory(staging, _installDirectory);
                _logger.Info($"Основная программа обновлена. Backup: {backup}");

                // Пакет, из которого выполнено обновление, в новой установке больше не нужен.
                TryDeleteDirectory(Path.Combine(_installDirectory, "updates", Path.GetFileName(sourceDir)));
                return true;
            }
            catch
            {
                if (Directory.Exists(_installDirectory))
                {
                    TryDeleteDirectory(_installDirectory);
                }

                if (Directory.Exists(backup))
                {
                    TryMoveDirectory(backup, _installDirectory);
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка обновления основной программы: {ex.Message}");
            return false;
        }
        finally
        {
            TryDeleteDirectory(staging);
        }
    }

    private static void CopyProtectedItems(string sourceInstall, string destinationInstall)
    {
        foreach (var dirName in new[] { "modules", "updates", "logs" })
        {
            var source = Path.Combine(sourceInstall, dirName);
            if (Directory.Exists(source))
            {
                CopyDirectory(source, Path.Combine(destinationInstall, dirName));
            }
        }

        var settings = Path.Combine(sourceInstall, "settings.yaml");
        if (File.Exists(settings))
        {
            File.Copy(settings, Path.Combine(destinationInstall, "settings.yaml"), true);
        }
    }

    private static void ValidateInstallDirectory(string directory)
    {
        foreach (var required in new[]
                 {
                     "OutfitTool.exe",
                     "OutfitTool.dll",
                     "OutfitTool.runtimeconfig.json",
                     "OutfitTool.deps.json"
                 })
        {
            var path = Path.Combine(directory, required);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Новая версия не содержит обязательный файл '{required}'.", path);
            }
        }
    }

    private bool RollbackMainModule()
    {
        if (_lastMainBackup == null || !Directory.Exists(_lastMainBackup)) return false;

        try
        {
            if (Directory.Exists(_installDirectory))
            {
                var failed = _installDirectory + "_failed_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                TryMoveDirectory(_installDirectory, failed);
            }

            MoveDirectory(_lastMainBackup, _installDirectory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Не удалось откатить основную программу: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Запуск

    private bool RestartMainProgram(Dictionary<string, string>? mainAppParameters)
    {
        _logger.Info("Запуск обновлённой программы...");

        if (!File.Exists(_mainExePath))
        {
            _logger.Error($"Основной EXE не найден: {_mainExePath}");
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _mainExePath,
            WorkingDirectory = _installDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (mainAppParameters != null)
        {
            foreach (var (key, value) in mainAppParameters)
            {
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;
                if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)) continue;
                startInfo.ArgumentList.Add($"{key}={value}");
            }
        }

        var process = Process.Start(startInfo);
        if (process == null)
        {
            _logger.Error("Process.Start вернул null при запуске основной программы.");
            return false;
        }

        Thread.Sleep(1500);
        if (process.HasExited)
        {
            _logger.Error($"Обновлённая программа завершилась с кодом {process.ExitCode}.");
            return false;
        }

        _logger.Info("Основная программа запущена.");
        return true;
    }

    #endregion

    #region Файловые операции

    private static string NormalizeDirectory(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            NormalizeDirectory(left),
            NormalizeDirectory(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSafeName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && SafeNameRegex.IsMatch(name);
    }

    private static bool IsPathUnder(string root, string path)
    {
        var normalizedRoot = NormalizeDirectory(root) + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path);
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyDirectory(
        string source,
        string destination,
        HashSet<string>? excludedAtRoot = null)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source))
        {
            var name = Path.GetFileName(file);
            if (excludedAtRoot != null && excludedAtRoot.Contains(name)) continue;
            File.Copy(file, Path.Combine(destination, name), true);
        }

        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            var name = Path.GetFileName(directory);
            if (excludedAtRoot != null && excludedAtRoot.Contains(name)) continue;

            var attributes = File.GetAttributes(directory);
            if ((attributes & FileAttributes.ReparsePoint) != 0) continue;

            CopyDirectory(directory, Path.Combine(destination, name));
        }
    }

    private static void MoveDirectory(string source, string destination)
    {
        if (Directory.Exists(destination))
        {
            throw new IOException($"Каталог назначения уже существует: {destination}");
        }

        Directory.Move(source, destination);
    }

    private static bool TryMoveDirectory(string source, string destination)
    {
        try
        {
            MoveDirectory(source, destination);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryDeleteDirectory(string path, int attempts = 1)
    {
        if (!Directory.Exists(path)) return true;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                Directory.Delete(path, true);
                return true;
            }
            catch (Exception ex)
            {
                if (attempt == attempts - 1)
                {
                    _logger.Warning($"Не удалось удалить каталог {path}: {ex.Message}");
                    return false;
                }

                Thread.Sleep(RetryDelayMs);
            }
        }

        return false;
    }

    private void CleanupOldDirectories(string root, TimeSpan maxAge, bool keepNewest = false)
    {
        if (!Directory.Exists(root)) return;

        try
        {
            var directories = Directory.GetDirectories(root)
                .Select(path => new DirectoryInfo(path))
                .OrderByDescending(info => info.LastWriteTimeUtc)
                .ToList();

            for (var i = 0; i < directories.Count; i++)
            {
                if (keepNewest && i == 0) continue;
                if (DateTime.UtcNow - directories[i].LastWriteTimeUtc < maxAge) continue;
                TryDeleteDirectory(directories[i].FullName);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"Очистка {root} не выполнена: {ex.Message}");
        }
    }

    #endregion
}
