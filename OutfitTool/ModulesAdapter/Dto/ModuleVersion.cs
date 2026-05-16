/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
namespace OutfitTool.ModulesAdapter.Dto;

public class ModuleVersion(
    int majorVersion,
    int minorVersion,
    string versionLabel = "")
{
    private readonly int _majorVersion = majorVersion;
    private readonly int _minorVersion = minorVersion;
    public readonly string VersionLabel = versionLabel;
    public string FullVersion { get {
            var res = _majorVersion + "." + _minorVersion;
            if (VersionLabel != "")
            {
                res += " " + VersionLabel;
            }
            return res;
        } 
    }


    public static ModuleVersion FromString(string version, string label = "")
    {
        return new ModuleVersion(
            int.Parse(version[..version.IndexOf('.')]),
            int.Parse(version[(version.IndexOf('.') + 1)..]),
            label
        );
    }

    public override string ToString()
    {
        return _majorVersion + "." + _minorVersion;
    }

    public static bool operator <(ModuleVersion a, ModuleVersion b)
    {
        return a._majorVersion * 10000 + a._minorVersion < b._majorVersion * 10000 + b._minorVersion;
    }
    public static bool operator >(ModuleVersion a, ModuleVersion b)
    {
        return a._majorVersion * 10000 + a._minorVersion > b._majorVersion * 10000 + b._minorVersion;
    }
    public static bool operator <=(ModuleVersion a, ModuleVersion b)
    {
        return a._majorVersion * 10000 + a._minorVersion <= b._majorVersion * 10000 + b._minorVersion;
    }
    public static bool operator >=(ModuleVersion a, ModuleVersion b)
    {
        return a._majorVersion * 10000 + a._minorVersion >= b._majorVersion * 10000 + b._minorVersion;
    }
    public static bool operator ==(ModuleVersion a, ModuleVersion b)
    {
        return a._majorVersion * 10000 + a._minorVersion == b._majorVersion * 10000 + b._minorVersion;
    }
    public static bool operator !=(ModuleVersion a, ModuleVersion b)
    {
        return a._majorVersion * 10000 + a._minorVersion != b._majorVersion * 10000 + b._minorVersion;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj);
    }

    public override int GetHashCode()
    {
        return _majorVersion + _minorVersion;
    }
}