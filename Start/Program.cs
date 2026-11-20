/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System;
using System.Diagnostics;
using System.IO;

namespace Start
{
    /// <summary>
    /// Legacy-загрузчик. Вся логика обновления перенесена в OutfitTool;
    /// Start только запускает основную программу и передаёт ей аргументы.
    /// </summary>
    internal static class Program
    {
        private static ILoggerInterface _logger = null!;

        private static int Main(string[] args)
        {
            _logger = new FileLogger();

            try
            {
                var appDirectory = AppContext.BaseDirectory;
                var mainExePath = Path.Combine(appDirectory, "OutfitTool.exe");

                if (!File.Exists(mainExePath))
                {
                    _logger.Error("OutfitTool.exe не найден: " + mainExePath);
                    return 1;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = mainExePath,
                    WorkingDirectory = appDirectory,
                    UseShellExecute = false
                };

                foreach (var argument in args)
                {
                    startInfo.ArgumentList.Add(argument);
                }

                _logger.Info("Запуск " + mainExePath);
                Process.Start(startInfo);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.Error("Ошибка запуска OutfitTool: " + ex);
                return 1;
            }
        }
    }
}
