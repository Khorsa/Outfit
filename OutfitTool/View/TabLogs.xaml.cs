/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.Services;
using System.IO;
using System.Windows.Controls;
using OutfitTool.ModulesAdapter.Logger;

namespace OutfitTool.View;

/// <summary>
/// Логика взаимодействия для TabLogs.xaml
/// </summary>
public partial class TabLogs : UserControl
{
    private const bool LogInFile = true;
    private const long MaxLogFileSize = 5 * 1024 * 1024;
    private static readonly object LogFileLock = new();
    private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Outfit.log");

    public TabLogs()
    {
        InitializeComponent();

        ServiceLocator.GetService<ILogger>().Subscribe(this.LogHandler);

        if (LogInFile){
            ServiceLocator.GetService<ILogger>().Subscribe(this.LogFileHandler);
        }
    }

    private void LogHandler(object? sender, LogEntry logEntry)
    {
        string message = logEntry.LogDateTime.ToString() + " " + logEntry.Level.ToString() + ": " + logEntry.Message;
        try
        {
            this.Dispatcher.Invoke(() => {
                logsBlock.Text += message + "\r\n";
            });
        }
        catch
        {
            // Окно логов может быть уже закрыто при завершении приложения.
        }
    }

    private void LogFileHandler(object? sender, LogEntry logEntry)
    {
        string message = logEntry.LogDateTime.ToString() + " " + logEntry.Level.ToString() + ": " + logEntry.Message;

        lock (LogFileLock)
        {
            try
            {
                if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > MaxLogFileSize)
                {
                    var oldPath = LogFilePath + ".old";
                    File.Copy(LogFilePath, oldPath, true);
                    File.WriteAllText(LogFilePath, string.Empty);
                }

                File.AppendAllText(LogFilePath, message + Environment.NewLine);
            }
            catch
            {
                // Логирование не должно ронять приложение.
            }
        }
    }
}