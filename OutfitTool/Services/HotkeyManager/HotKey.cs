/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.Windows.Input;

namespace OutfitTool.Services.HotkeyManager;

public class HotKey
{
    public Key KeyCode;
    public ModifierKeys KeyMod;

    public HotKey(Key keyCode, ModifierKeys keyMod)
    {
        this.KeyCode = keyCode;
        this.KeyMod = keyMod;
    }

    public override string ToString() { 
        if (KeyMod == ModifierKeys.None)
        {
            return KeyCode.ToString();
        }
        else
        {
            return KeyMod.ToString().Replace(" ", "") + '+' + KeyCode.ToString();
        }
    }

    public static HotKey? FromString(string hotKeyString)
    {
        HotKey? result = null;

        string[] divided = hotKeyString.Trim().Split('+');
        Key key;
        ModifierKeys keyMod = ModifierKeys.None;

        if (
            divided.Length == 2 
            && Enum.TryParse<Key>(divided[1], out key) 
            && Enum.TryParse<ModifierKeys>(divided[0], out keyMod)
            ||
            divided.Length == 1 
            && Enum.TryParse<Key>(divided[0], out key)
        )
        {
            return new HotKey(key, keyMod);
        }

        return result;
    }

    public static bool operator ==(HotKey? left, HotKey? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;

        return left.KeyCode == right.KeyCode && left.KeyMod == right.KeyMod;
    }

    public static bool operator !=(HotKey? left, HotKey? right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is HotKey other)
        {
            return this == other;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(KeyCode, KeyMod);
    }
}