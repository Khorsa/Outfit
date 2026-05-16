/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.ModulesAdapter;
using OutfitTool.Services.HotkeyManager;

namespace OutfitTool.Services.Settings;

public class HotKeyLink(HotKey key, CommandDescriptor command) : IStringSerializable
{
    public HotKey Key { get; } = key;
    public CommandDescriptor Command { get; } = command;

    public IStringSerializable FromString(string input)
    {
        return HotKeyLinkSerializer.Deserialize(input);
    }

    public override string ToString()
    {
        return HotKeyLinkSerializer.Serialize(this);
    }
}