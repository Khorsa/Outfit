using System.Diagnostics;
using System.IO;
using OutfitTool.ModulesAdapter.Logger;

namespace OutfitTool.Services.Updates;

/// <summary>
/// Класс для самообновления программы
/// </summary>
public class FilesUpdater
{
    private readonly ILogger _logger;
    private readonly string _appDirectory;
    private readonly string _cacheDirectory;
    private readonly string _updatesDirectory;
    private readonly string _modulesDirectory;
    private readonly string _mainExePath;
    private readonly string _mainProcessName;
    
    // Конфигурация
    private const int WAIT_FOR_EXIT_TIMEOUT_SECONDS = 30;
    private const int RETRY_DELAY_MS = 200;
    private const int MAX_RETRY_ATTEMPTS = 150;
    
    // Файлы и папки, которые нужно копировать в cache
    private readonly List<string> _filesToCopy =
    [
        "OutfitTool.exe",
        "OutfitTool.runtimeconfig.json"
        // Добавьте сюда все файлы, которые нужны для работы апдейтера (dll рядом с exe скопируются сами)
    ];
    
    // Файлы/папки, которые нельзя удалять при очистке основной директории
    private readonly List<string> _protectedItems =
    [
        "modules",
        "updates",
        "settings.yaml",
        "logs",
        "update_cache"
    ];

    public FilesUpdater(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _cacheDirectory = Path.Combine(_appDirectory, "update_cache");
        _updatesDirectory = Path.Combine(_appDirectory, "updates");
        _modulesDirectory = Path.Combine(_appDirectory, "modules");
        _mainExePath = Path.Combine(_appDirectory, "OutfitTool.exe");
        _mainProcessName = "OutfitTool";
    }

    /// <summary>
    /// Метод 1: Перезапуск программы с обновлением
    /// </summary>
    /// <param name="modulesToDelete">Список модулей для удаления</param>
    /// <param name="mainAppParameters">Параметры для передачи основной программе после обновления</param>
    public void Restart(List<string> modulesToDelete, Dictionary<string, string>? mainAppParameters = null)
    {
        _logger.Info("=== Начало процесса обновления ===");
        
        try
        {
            // 1. Создаём кэш директорию
            CreateCacheDirectory();
            
            // 2. Копируем необходимые файлы в кэш
            CopyFilesToCache();
            
            // 3. Формируем параметры для запуска копии
            string arguments = BuildUpdateArguments(modulesToDelete, mainAppParameters);
            
            // 4. Запускаем копию программы
            string cacheExePath = Path.Combine(_cacheDirectory, "OutfitTool.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = cacheExePath,
                Arguments = arguments,
                UseShellExecute = true,
                WorkingDirectory = _cacheDirectory
            };
            
            _logger.Info($"Запуск копии программы: {cacheExePath} {arguments}");
            Process.Start(startInfo);
            
            // 5. Завершаем текущую программу
            _logger.Info("Завершение текущего экземпляра программы");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка при подготовке к обновлению: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Метод 2: Выполнение обновления (запускается из копии с параметром --update)
    /// </summary>
    /// <param name="modulesToDelete">Список модулей для удаления</param>
    /// <param name="mainAppParameters">Параметры для передачи основной программе после обновления</param>
    public void Update(List<string> modulesToDelete, Dictionary<string, string>? mainAppParameters = null)
    {
        _logger.Info("=== Запущен режим обновления ===");
        
        try
        {
            // 1. Ожидаем закрытия основной программы
            WaitForMainProgramExit();
            
            // 2. Удаляем указанные модули
            DeleteModules(modulesToDelete);
            
            // 3. Обрабатываем обновления из папки updates
            ProcessUpdatesDirectory();
            
            // 4. Запускаем обновлённую программу с переданными параметрами
            RestartMainProgram(mainAppParameters);
            
            _logger.Info("=== Обновление успешно завершено ===");
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка в режиме обновления: {ex.Message}");
            throw;
        }
        finally
        {
            Environment.Exit(0);
        }
    }

    /// <summary>
    /// Очистка кэш директории при запуске основной программы
    /// </summary>
    public void CleanupCache()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            try
            {
                _logger.Info($"Очистка кэш директории: {_cacheDirectory}");
                Directory.Delete(_cacheDirectory, true);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Не удалось очистить кэш: {ex.Message}");
            }
        }
    }

    #region Приватные методы для Restart

    private void CreateCacheDirectory()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            _logger.Info($"Удаление старой кэш директории: {_cacheDirectory}");
            Directory.Delete(_cacheDirectory, true);
        }
        
        _logger.Info($"Создание кэш директории: {_cacheDirectory}");
        Directory.CreateDirectory(_cacheDirectory);
    }

    private void CopyFilesToCache()
    {
        _logger.Info("Копирование файлов в кэш...");
        
        foreach (string filePattern in _filesToCopy)
        {
            string sourcePath = Path.Combine(_appDirectory, filePattern);
            string destPath = Path.Combine(_cacheDirectory, filePattern);
            
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, destPath, true);
                _logger.Info($"Скопирован: {filePattern}");
            }
            else
            {
                _logger.Warning($"Файл не найден: {filePattern}");
            }
        }
        
        // Копируем также все DLL из корневой директории
        var dllFiles = Directory.GetFiles(_appDirectory, "*.dll");
        foreach (var dll in dllFiles)
        {
            string fileName = Path.GetFileName(dll);
            string destPath = Path.Combine(_cacheDirectory, fileName);
            File.Copy(dll, destPath, true);
            _logger.Info($"Скопирован DLL: {fileName}");
        }
    }

    private string BuildUpdateArguments(List<string> modulesToDelete, Dictionary<string, string>? mainAppParameters)
    {
        var args = new List<string> { "--update" };
        
        // Добавляем модули для удаления
        foreach (var module in modulesToDelete)
        {
            args.Add($"delete_module={module}");
        }
        
        // Сохраняем параметры основной программы в JSON (чтобы передать сложные данные)
        if (mainAppParameters is { Count: > 0 })
        {
            string parametersJson = System.Text.Json.JsonSerializer.Serialize(mainAppParameters);
            // Кодируем для передачи в командной строке
            string encodedParams = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(parametersJson));
            args.Add($"main_params={encodedParams}");
        }
        
        return string.Join(" ", args);
    }

    #endregion

    #region Приватные методы для Update

    private void WaitForMainProgramExit()
    {
        _logger.Info($"Ожидание завершения процесса {_mainProcessName}...");
        
        int waitSeconds = 0;
        int maxRetries = WAIT_FOR_EXIT_TIMEOUT_SECONDS * 5; // *5 потому что ждём по 200ms
        
        while (waitSeconds < maxRetries)
        {
            var processes = Process.GetProcessesByName(_mainProcessName);
            var currentProcess = Process.GetCurrentProcess();
            var mainProcess = processes.FirstOrDefault(p => p.Id != currentProcess.Id);
            
            if (mainProcess == null)
            {
                _logger.Info("Основная программа не запущена");
                Thread.Sleep(1000); // Дополнительная задержка для освобождения файлов
                return;
            }
            
            if (mainProcess.HasExited)
            {
                _logger.Info("Основная программа завершилась");
                Thread.Sleep(1000);
                return;
            }
            
            if (waitSeconds % 10 == 0) // Логируем каждые 2 секунды
            {
                _logger.Info($"Ожидание (PID: {mainProcess.Id})...");
            }
            
            Thread.Sleep(RETRY_DELAY_MS);
            waitSeconds++;
        }
        
        _logger.Warning($"Таймаут ожидания завершения программы ({WAIT_FOR_EXIT_TIMEOUT_SECONDS} сек)");
    }

    private void DeleteModules(List<string> modulesToDelete)
    {
        foreach (var module in modulesToDelete)
        {
            _logger.Info($"Удаление модуля: {module}...");
            
            string moduleDir = Path.Combine(_modulesDirectory, module);
            if (!Directory.Exists(moduleDir))
            {
                _logger.Warning($"Модуль не найден: {moduleDir}");
                continue;
            }
            
            bool deleted = false;
            int tryCounts = 0;
            int maxRetries = MAX_RETRY_ATTEMPTS;
            
            do
            {
                try
                {
                    Directory.Delete(moduleDir, true);
                    deleted = true;
                    _logger.Info($"Модуль удалён: {module}");
                }
                catch (Exception ex)
                {
                    tryCounts++;
                    _logger.Warning($"Ошибка удаления модуля (попытка {tryCounts}): {ex.Message}");
                    Thread.Sleep(RETRY_DELAY_MS);
                }
            }
            while (!deleted && tryCounts < maxRetries);
            
            if (!deleted)
            {
                _logger.Error($"Не удалось удалить модуль {module} после {maxRetries} попыток");
            }
        }
    }

    private void ProcessUpdatesDirectory()
    {
        if (!Directory.Exists(_updatesDirectory))
        {
            _logger.Info("Папка updates не существует, пропускаем");
            return;
        }
        
        var subDirs = Directory.GetDirectories(_updatesDirectory);
        _logger.Info($"Найдено {subDirs.Length} папок обновлений");
        
        foreach (var dir in subDirs)
        {
            string dirName = Path.GetFileName(dir);
            _logger.Info($"Обработка: {dirName}");
            
            if (dirName == _mainProcessName || dirName == Path.GetFileNameWithoutExtension(_mainExePath))
            {
                // Обновление основного модуля
                UpdateMainModule(dir);
            }
            else
            {
                // Обновление дополнительного модуля
                UpdateAdditionalModule(dir, dirName);
            }
            
            // Удаляем обработанную папку обновления
            try
            {
                Directory.Delete(dir, true);
                _logger.Info($"Удалена папка обновления: {dirName}");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Не удалось удалить папку {dirName}: {ex.Message}");
            }
        }
    }

    private void UpdateMainModule(string sourceDir)
    {
        _logger.Info("Обновление основного модуля...");
        
        // Очищаем основную директорию, защищая важные файлы
        bool cleared;
        var tryCounts = 0;

        do
        {
            _logger.Info("Очистка основной директории...");
            cleared = ClearMainDirectory();
            if (cleared) continue;
            _logger.Warning($"Ошибка очистки (попытка {tryCounts + 1})");
            Thread.Sleep(RETRY_DELAY_MS);
            tryCounts++;
        }
        while (!cleared && tryCounts < MAX_RETRY_ATTEMPTS);
        
        if (!cleared)
        {
            _logger.Warning("Не удалось полностью очистить директорию, продолжаем...");
        }
        
        _logger.Info("Копирование файлов основного модуля...");
        DirectoryRecursiveCopy(sourceDir, _appDirectory);
    }

    private void UpdateAdditionalModule(string sourceDir, string moduleName)
    {
        _logger.Info($"Обновление модуля: {moduleName}");
        
        string destDir = Path.Combine(_modulesDirectory, moduleName);
        
        // Удаляем старую версию модуля
        if (Directory.Exists(destDir))
        {
            bool deleted = false;
            int tryCounts = 0;
            int maxRetries = MAX_RETRY_ATTEMPTS;
            
            do
            {
                try
                {
                    _logger.Info($"Удаление старой версии модуля {moduleName}");
                    Directory.Delete(destDir, true);
                    deleted = true;
                }
                catch (Exception ex)
                {
                    tryCounts++;
                    _logger.Warning($"Ошибка удаления (попытка {tryCounts}): {ex.Message}");
                    Thread.Sleep(RETRY_DELAY_MS);
                }
            }
            while (!deleted && tryCounts < maxRetries);
        }
        
        // Копируем новый модуль
        _logger.Info($"Копирование модуля {moduleName}");
        DirectoryRecursiveCopy(sourceDir, destDir);
    }

    private bool ClearMainDirectory()
    {
        try
        {
            var items = Directory.GetFileSystemEntries(_appDirectory);
            
            foreach (var item in items)
            {
                var name = Path.GetFileName(item);
                
                if (name == "." || name == "..")
                    continue;
                
                if (_protectedItems.Contains(name))
                {
                    _logger.Info($"Защищённый элемент пропущен: {name}");
                    continue;
                }
                
                if (Directory.Exists(item))
                {
                    Directory.Delete(item, true);
                    _logger.Info($"Удалена папка: {name}");
                }
                else
                {
                    File.Delete(item);
                    _logger.Info($"Удалён файл: {name}");
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Ошибка при очистке директории: {ex.Message}");
            return false;
        }
    }

    private void DirectoryRecursiveCopy(string sourceDirName, string destDirName)
    {
        var sourceDir = new DirectoryInfo(sourceDirName);
        if (!sourceDir.Exists)
        {
            _logger.Warning($"Исходная папка не найдена: {sourceDirName}");
            return;
        }
        
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }
        
        // Копируем файлы
        foreach (var file in sourceDir.GetFiles())
        {
            string destPath = Path.Combine(destDirName, file.Name);
            if (File.Exists(destPath))
            {
                // Файл уже существует и не был удалён при очистке
                _logger.Info($"Файл пропущен (уже существует): {file.Name}");
                continue;
            }
            file.CopyTo(destPath, false);
            _logger.Info($"Скопирован файл: {file.Name}");
        }
        
        // Копируем подпапки
        foreach (var dir in sourceDir.GetDirectories())
        {
            string destSubPath = Path.Combine(destDirName, dir.Name);
            DirectoryRecursiveCopy(dir.FullName, destSubPath);
        }
    }

    private void RestartMainProgram(Dictionary<string, string>? mainAppParameters)
    {
        _logger.Info("Запуск обновлённой программы...");
        
        if (!File.Exists(_mainExePath))
        {
            _logger.Error($"Основной EXE не найден: {_mainExePath}");
            return;
        }
        
        // Формируем аргументы командной строки
        string arguments = "";
        if (mainAppParameters is { Count: > 0 })
        {
            var args = new List<string>();
            foreach (var param in mainAppParameters)
            {
                args.Add($"{param.Key}={param.Value}");
            }
            arguments = string.Join(" ", args);
            _logger.Info($"Параметры запуска: {arguments}");
        }
        
        var startInfo = new ProcessStartInfo
        {
            FileName = _mainExePath,
            Arguments = arguments,
            UseShellExecute = true,
            WorkingDirectory = _appDirectory
        };
        
        Process.Start(startInfo);
        _logger.Info("Основная программа запущена");
    }

    #endregion
}
