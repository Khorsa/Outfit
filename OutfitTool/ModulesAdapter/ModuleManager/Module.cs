/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
namespace OutfitTool.ModulesAdapter.ModuleManager;

public class Module(
    string assemblyName,
    IModuleInfo moduleInfo,
    IModuleController moduleController)
{
    public IModuleInfo ModuleInfo { get; } = moduleInfo;
    public IModuleController ModuleController { get; } = moduleController;
    public string AssemblyName { get; } = assemblyName;
    public bool Enabled { get; set; }
}