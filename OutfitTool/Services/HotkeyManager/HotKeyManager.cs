/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.Windows;
using OutfitTool.ModulesAdapter;
using OutfitTool.ModulesAdapter.Logger;
using OutfitTool.ModulesAdapter.ModuleManager;
using OutfitTool.Services.Settings;

namespace OutfitTool.Services.HotkeyManager;

public class HotKeyManager(SettingsManager<AppSettings> settingsManager)
{
    private HotKeyRegisterService? _hotKeyRegisterService;

    public void Init(IntPtr windowHandle)
    {
        _hotKeyRegisterService = new HotKeyRegisterService(windowHandle);

        var settings = settingsManager.LoadSettings();
        var settingHotKeys = settings.HotKeys?.ToDictionary() ?? new Dictionary<string, string>();

        foreach (var (commandString, hotKey) in settingHotKeys)
        {
            try
            {
                var moduleManager = ServiceLocator.GetService<IModuleManager>();
                var commandDescriptor = moduleManager.GetCommandDescriptor(commandString);
                var key = HotKey.FromString(hotKey);

                if (key == null || commandDescriptor == null)
                {
                    ServiceLocator.GetService<ILogger>().Warning(
                        $"Пропущена горячая клавиша '{commandString}': команда или комбинация не найдены.");
                    continue;
                }

                ServiceLocator.GetService<ILogger>().Info("Регистрация горячей клавиши " + key);
                if (!_hotKeyRegisterService.Register(key))
                {
                    ServiceLocator.GetService<ILogger>().Warning(
                        $"Горячая клавиша {key} уже зарегистрирована; команда '{commandString}' пропущена.");
                }
            }
            catch (Exception ex)
            {
                ServiceLocator.GetService<ILogger>().Error(
                    $"Ошибка регистрации горячей клавиши '{commandString}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Назначает комбинацию команде. Возвращает false, если комбинация занята или сохранение не удалось.
    /// </summary>
    public bool RegisterKey(CommandDescriptor commandDescriptor, HotKey key)
    {
        return RegisterKeyInternal(commandDescriptor, key, saveSettings: true);
    }

    private bool RegisterKeyInternal(CommandDescriptor commandDescriptor, HotKey key, bool saveSettings)
    {
        if (_hotKeyRegisterService == null)
        {
            throw new InvalidOperationException("Сервис горячих клавиш не инициализирован.");
        }

        var settings = settingsManager.LoadSettings();
        settings.HotKeys ??= new Dictionary<string, string>();

        var commandString = commandDescriptor.ToString();
        var previousKey = GetKey(commandDescriptor);

        try
        {
            // Сначала снимаем старую комбинацию этой же команды, чтобы не держать её в ОС.
            if (previousKey != null)
            {
                _hotKeyRegisterService.Unregister(previousKey);
            }

            if (!_hotKeyRegisterService.Register(key))
            {
                ShowError($"Горячая клавиша {key} уже назначена другой команде.");
                RestorePreviousKey(commandDescriptor, previousKey);
                return false;
            }

            RemoveCommandEntries(settings, commandDescriptor);
            settings.HotKeys[commandString] = key.ToString();

            if (saveSettings)
            {
                try
                {
                    settingsManager.SaveSettings(settings);
                }
                catch
                {
                    _hotKeyRegisterService.Unregister(key);
                    RestorePreviousKey(commandDescriptor, previousKey);
                    throw;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            RestorePreviousKey(commandDescriptor, previousKey);
            ServiceLocator.GetService<ILogger>().Error($"Ошибка назначения горячей клавиши {key}: {ex.Message}");
            ShowError(ex.Message);
            return false;
        }
    }

    public void ClearKey(CommandDescriptor commandDescriptor)
    {
        if (_hotKeyRegisterService == null)
        {
            throw new InvalidOperationException("Сервис горячих клавиш не инициализирован.");
        }

        var settings = settingsManager.LoadSettings();
        settings.HotKeys ??= new Dictionary<string, string>();

        var key = GetKey(commandDescriptor);
        if (key != null)
        {
            _hotKeyRegisterService.Unregister(key);
        }

        RemoveCommandEntries(settings, commandDescriptor);
        settingsManager.SaveSettings(settings);
    }

    public void ClearAllKeys()
    {
        if (_hotKeyRegisterService == null) return;
        _hotKeyRegisterService.UnregisterAll();
    }

    public HotKey? GetKey(CommandDescriptor command)
    {
        var settings = settingsManager.LoadSettings();
        if (settings.HotKeys == null) return null;

        foreach (var entry in settings.HotKeys)
        {
            if (command.Matches(entry.Key))
            {
                return HotKey.FromString(entry.Value.Trim());
            }
        }

        return null;
    }

    public void HotKeyPressed(int hotKeyId)
    {
        var key = _hotKeyRegisterService?.GetById(hotKeyId);
        if (key == null) return;

        var settings = settingsManager.LoadSettings();
        if (settings.HotKeys == null) return;

        string? commandString = null;
        foreach (var entry in settings.HotKeys)
        {
            if (string.Equals(entry.Value.Trim(), key.ToString(), StringComparison.Ordinal))
            {
                commandString = entry.Key.Trim();
                break;
            }
        }

        if (commandString == null) return;

        try
        {
            IModuleManager moduleManager = ServiceLocator.GetService<IModuleManager>();
            CommandDescriptor? command = moduleManager.GetCommandDescriptor(commandString);
            command?.Command.Run();
        }
        catch (Exception ex)
        {
            ServiceLocator.GetService<ILogger>().Error(
                $"Ошибка выполнения команды по горячей клавише '{commandString}': {ex}");
        }
    }

    private void RestorePreviousKey(CommandDescriptor commandDescriptor, HotKey? previousKey)
    {
        var settings = settingsManager.LoadSettings();
        settings.HotKeys ??= new Dictionary<string, string>();

        RemoveCommandEntries(settings, commandDescriptor);

        if (previousKey != null)
        {
            settings.HotKeys[commandDescriptor.ToString()] = previousKey.ToString();
            try { _hotKeyRegisterService?.Register(previousKey); } catch { /* ignored */ }
        }

        try { settingsManager.SaveSettings(settings); } catch { /* ignored */ }
    }

    private static void RemoveCommandEntries(AppSettings settings, CommandDescriptor commandDescriptor)
    {
        if (settings.HotKeys == null) return;

        foreach (var key in settings.HotKeys.Keys
                     .Where(existing => commandDescriptor.Matches(existing))
                     .ToList())
        {
            settings.HotKeys.Remove(key);
        }
    }

    private static void ShowError(string message)
    {
        MessageBox.Show(message, "Ошибка регистрации горячих клавиш", MessageBoxButton.OK, MessageBoxImage.Exclamation);
    }
}
