/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.ModulesAdapter.Dto;

namespace OutfitTool.ModulesAdapter;

public interface IModuleInfo
{
    string Name { get; }
    string DisplayName { get; }
    string AssemblyName { get; }
    string Description { get; }
    ModuleVersion Version { get; }
    int RequireApiVersion { get; }
    string Changes { get; }
    string Author { get; }
    string AuthorContacts { get; }
}