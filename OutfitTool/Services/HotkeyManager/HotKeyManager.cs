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

        var settingHotKeys = settingsManager.LoadSettings().HotKeys.ToDictionary();

        foreach (var (commandString, hotKey) in settingHotKeys)
        {
            var moduleManager = ServiceLocator.GetService<IModuleManager>();
            var commandDescriptor = moduleManager.GetCommandDescriptor(commandString);
            var key = HotKey.FromString(hotKey);
            if (key == null || commandDescriptor == null) continue;
            var logger = ServiceLocator.GetService<ILogger>();
            logger.Info("Регистрация горячей клавиши " + key);
            ClearKey(commandDescriptor);
            RegisterKey(commandDescriptor, key);
        }
    }


    public void RegisterKey(CommandDescriptor commandDescriptor, HotKey key)
    {
        var settings = settingsManager.LoadSettings();

        try
        {
            if (_hotKeyRegisterService == null) throw new Exception("Registration hotkey error");
            _hotKeyRegisterService.Register(key);
            settings.HotKeys.Remove(commandDescriptor.ToString());
            settings.HotKeys.Add(commandDescriptor.ToString(), key.ToString());
            settingsManager.SaveSettings(settings);
        }
        catch (Exception e) {
            MessageBox.Show(e.Message, "Ошибка регистрации горячих клавиш", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
    }

    public void ClearKey(CommandDescriptor commandDescriptor)
    {
        if (_hotKeyRegisterService == null) throw new Exception("Clear hotkey error");

        var settings = settingsManager.LoadSettings();
        var key = GetKey(commandDescriptor);
        if (key != null)
        {
            _hotKeyRegisterService.Unregister(key);
            settings.HotKeys.Remove(commandDescriptor.ToString());
        }
        settingsManager.SaveSettings(settings);
    }

    public void ClearAllKeys()
    {
        if (_hotKeyRegisterService == null) throw new Exception("Clear hotkey error");

        var settings = settingsManager.LoadSettings();
        foreach (var entry in settings.HotKeys)
        {
            var hotKey = HotKey.FromString(entry.Value.Trim());
            if(hotKey != null){
                _hotKeyRegisterService.Unregister(hotKey);
            }
        }
    }


    public HotKey? GetKey(CommandDescriptor command)
    {
        AppSettings settings = settingsManager.LoadSettings();

        foreach (var entry in settings.HotKeys)
        {
            var commandString = entry.Key.Trim();
            var hotKeyString = entry.Value.Trim();

            if (command.ToString() == commandString)
            {
                return HotKey.FromString(hotKeyString);
            }

        }
        return null;
    }


    public void HotKeyPressed(int hotKeyId)
    {
        var settings = settingsManager.LoadSettings();
        var key = _hotKeyRegisterService?.GetById(hotKeyId);

        if (key == null)
        {
            return;
        }

        string? commandString = null;

        foreach(KeyValuePair<string, string> entry in settings.HotKeys)
        {
            if (entry.Value == key.ToString())
            {
                commandString = entry.Key.Trim();
            }
        }

        if (commandString == null)
        {
            return;
        }

        IModuleManager moduleManager = ServiceLocator.GetService<IModuleManager>();
        CommandDescriptor? command = moduleManager.GetCommandDescriptor(commandString);

        if (command != null)
        {
            command.Command.Run();
        }
    }
}