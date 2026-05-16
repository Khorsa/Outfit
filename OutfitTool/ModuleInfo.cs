/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.ModulesAdapter;
using OutfitTool.ModulesAdapter.Dto;

namespace OutfitTool;

public class ModuleInfo : IModuleInfo
{
    public string Name => "outfit_tool";
    public string DisplayName => "Outfit Tool";
    public string AssemblyName => "OutfitTool";
    public string Description => "Outfit Tool";
    public ModuleVersion Version => new(3, 0, "release");
    ModuleVersion IModuleInfo.Version => throw new NotImplementedException();
    public string Changes => "3.0 release";
    public string Author => "Stolyarov Roman";
    public string AuthorContacts => "rshome@mail.ru";
    public int RequireApiVersion => 1;

    public readonly int[] SupportedApiVersions = [1];
}