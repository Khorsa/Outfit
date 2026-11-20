/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.Windows.Input;
using OutfitTool.ModulesAdapter.ModuleManager;
using OutfitTool.Services.HotkeyManager;
using OutfitTool.Services.Settings;
using ICommand = OutfitTool.ModulesAdapter.ICommand;

namespace OutfitTool.Services;

static class HotKeyLinkSerializer
{
    public static string Serialize(HotKeyLink link)
    {
        return $"{link.Key.KeyMod}+{link.Key.KeyCode}-{link.Command.Module.ModuleInfo.Name}.{link.Command.Command.Name}";
    }

    public static HotKeyLink Deserialize(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            throw new FormatException("Пустая строка HotKeyLink.");
        }

        var separator = serialized.IndexOf('-');
        if (separator <= 0 || separator == serialized.Length - 1)
        {
            throw new FormatException("Неверный формат сериализованной HotKeyLink: " + serialized);
        }

        var keyPart = serialized[..separator];
        var commandPart = serialized[(separator + 1)..];

        var keyStrings = keyPart.Split('+');
        if (keyStrings.Length != 2
            || !Enum.TryParse(keyStrings[0], out ModifierKeys modifier)
            || !Enum.TryParse(keyStrings[1], out Key keyCode))
        {
            throw new FormatException("Неверный формат горячей клавиши: " + keyPart);
        }

        var commandSeparator = commandPart.IndexOf('.');
        if (commandSeparator <= 0 || commandSeparator == commandPart.Length - 1)
        {
            throw new FormatException("Неверный формат команды: " + commandPart);
        }

        var moduleName = commandPart[..commandSeparator];
        var commandName = commandPart[(commandSeparator + 1)..];

        var moduleManager = ServiceLocator.GetService<IModuleManager>();
        var command = moduleManager.GetCommandDescriptor(moduleName + "." + commandName);
        if (command == null)
        {
            throw new InvalidOperationException($"Не найдена команда {moduleName}.{commandName}");
        }

        return new HotKeyLink(new HotKey(keyCode, modifier), command);
    }
}
