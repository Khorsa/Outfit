/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Start
{
    class Program
    {
        private static ILoggerInterface _logger;

        static void Main(string[] args)
        {
            _logger = new FileLogger();

            // Парсим параметры
            _logger.Info("Parsing parameters");
            var parameters = new Dictionary<string, string>();
            foreach (var arg in args)
            {
                var parts = arg.Split('=');
                if (parts.Length == 2)
                {
                    parameters.Add(parts[0], parts[1]);
                }
            }

            var deleteModules = new List<string>();
            var mainAppParameters = new Dictionary<string, string>();
            foreach (var param in parameters) 
            { 
                if (param.Key == "delete_module")
                {
                    _logger.Info("Need to delete module " + param.Value);
                    deleteModules.Add(param.Value);
                } else
                {
                    _logger.Info("Found parameters " + param.Key + " = " + param.Value);
                    mainAppParameters.Add(param.Key, param.Value);
                }
            }

            var appDirectory = AppContext.BaseDirectory;
            var updatesDirectory = Path.Combine(appDirectory, "updates");
            var modulesDirectory = Path.Combine(appDirectory, "modules");
            var mainExePath = Path.Combine(appDirectory, "OutfitTool.exe");
            var mainModuleAssemblyName = Path.Combine(appDirectory, "OutfitTool");
            List<string> mainModuleImportantFiles = new List<string>{"modules","Start.exe","updates","settings.yaml"};
            var secondsToWaitFreeResources = 20;

            // Удаляем указанные модули
            foreach(var module in deleteModules)
            {
                _logger.Info("Deleting module " + module + "...");

                var moduleDir = Path.Combine(modulesDirectory, module);
                if (Directory.Exists(moduleDir))
                {
                    var deleted = false;
                    var tryCounts = 0;
                    do
                    {
                        try
                        {
                            Directory.Delete(moduleDir, true);
                            deleted = true;
                            _logger.Info("Module deleted " + module);
                        }
                        catch
                        {
                            _logger.Warning("Module deletion error, waiting...");
                            Thread.Sleep(200);
                            tryCounts++;
                        }
                    } while (!deleted && tryCounts < secondsToWaitFreeResources / 0.2);
                }
            }

            _logger.Info("Checking directory " + updatesDirectory + "...");

            // Обновляем/инсталлируем новые модули
            if (Directory.Exists(updatesDirectory))
            {
                var subDirs = Directory.GetDirectories(updatesDirectory);

                _logger.Info("Found subdirs " + subDirs.Length);

                foreach (var dir in subDirs)
                {
                    _logger.Info("Checking module dir " + dir);
                    string dirName = Path.GetFileName(dir);

                    if (dirName == Path.GetFileNameWithoutExtension(mainModuleAssemblyName))
                    {

                        _logger.Info("Updating main module");

                        // Это основной модуль, его файлы нужно откопировать на место основной программы
                        bool deleted = false;
                        int tryCounts = 0;
                        do
                        {
                            _logger.Info("Clearing main directory");
                            deleted = ClearMainDirectory(appDirectory, mainModuleImportantFiles);
                            if (!deleted)
                            {
                                _logger.Warning("Clearing main directory error, waiting...");
                                Thread.Sleep(200);
                                tryCounts++;
                            }
                        } while (!deleted && tryCounts < secondsToWaitFreeResources / 0.2);
                        _logger.Info("Recursive copying main directory files");
                        DirectoryRecursiveCopy(dir, appDirectory);
                    }
                    else
                    {
                        // Это модуль, его нужно откопировать в директорию modules с заменой
                        _logger.Info("Updating modules " + dirName);
                        string moduleDir = Path.Combine(modulesDirectory, dirName);
                        if (Directory.Exists(moduleDir))
                        {
                            bool deleted = false;
                            int tryCounts = 0;
                            do
                            {
                                try
                                {
                                    _logger.Info("Deleting module "+ dirName + " directory");
                                    Directory.Delete(moduleDir, true);
                                    deleted = true;
                                } 
                                catch
                                {
                                    _logger.Warning("Deleting module " + dirName + " directory error, waiting...");
                                    Thread.Sleep(200);
                                    tryCounts++;
                                }
                            } while (!deleted && tryCounts < secondsToWaitFreeResources / 0.2);
                        }

                        string destDir = Path.Combine(modulesDirectory, dirName);
                        _logger.Info("Copying module files");

                        DirectoryRecursiveCopy(dir, destDir);

                    }
                    Directory.Delete(dir, true);
                }

                var startInfo = new ProcessStartInfo(mainExePath)
                {
                    Arguments = DictionaryToString(mainAppParameters),
                    UseShellExecute = true
                };
                _logger.Info("Starting main program");
                Process.Start(startInfo);
            }
        }

        static string DictionaryToString(Dictionary<string, string> dict)
        {
            List<string> pairs = new List<string>();
            foreach (var kvp in dict)
            {
                pairs.Add($"{kvp.Key}={kvp.Value}");
            }
            return string.Join(" ", pairs);
        }

        private static bool ClearMainDirectory(string mainDirectory, List<string> exclude)
        {
            try
            {
                var items = Directory.GetFileSystemEntries(mainDirectory);

                foreach (var item in items)
                {
                    var name = Path.GetFileName(item);

                    if (name == "." || name == "..")
                    {
                        continue;
                    }

                    if (!exclude.Contains(name))
                    {
                        if (Directory.Exists(item))
                        {
                            Directory.Delete(item, true);
                        }
                        else
                        {
                            File.Delete(item);
                        }
                    }
                }
                return true;
            } catch { 
            }
            return false;
        }

        private static void DirectoryRecursiveCopy(string sourceDirName, string destDirName)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = sourceDir.GetDirectories();

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = sourceDir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                if (File.Exists(temppath))
                {
                    // Это файл, который мы не удалили при очистке директории; его не надо заменять.
                    continue;
                }
                file.CopyTo(temppath, false);
            }

            foreach (DirectoryInfo dir in dirs)
            {
                string temppath = Path.Combine(destDirName, dir.Name);
                DirectoryRecursiveCopy(dir.FullName, temppath);
            }
        }
    }
}
