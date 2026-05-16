/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.ModulesAdapter.Dto;

namespace OutfitTool.ModulesAdapter.Adapters.v1;

public class ModuleInfoV1 : IModuleInfo
{
    OutfitTool.API1.IModuleInfo _api;

    public ModuleInfoV1(OutfitTool.API1.IModuleInfo api)
    {
        this._api = api;
    }

    public string Name => this._api.Name;

    public string DisplayName => this._api.DisplayName;

    public string AssemblyName => this._api.AssemblyName;

    public string Description => this._api.Description;

    public ModuleVersion Version => new ModuleVersion(_api.Version.MajorVersion, _api.Version.MinorVersion);

    public int RequireApiVersion => this._api.RequireApiVersion;

    public string Changes => this._api.Changes;

    public string Author => this._api.Author;

    public string AuthorContacts => this._api.AuthorContacts;

}