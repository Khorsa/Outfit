/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.ModulesAdapter.Dto;
using OutfitTool.Services.Updates;

namespace OutfitTool.View;

public class LastCompatibleRepositoryItem : RepositoryItem
{
    public bool CanInstall { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool HasInstalled { get; set; }
    public LastCompatibleRepositoryItemStatus Status { get; set; }

    public ModuleVersion? InstalledVersion { get; set; }

    public LastCompatibleRepositoryItem(): base(){}

    public LastCompatibleRepositoryItem(RepositoryItem item)
    {
        this.Name = item.Name;
        this.AssemblyName = item.AssemblyName;
        this.DisplayName = item.DisplayName;
        this.Description = item.Description;
        this.Version = item.Version;
        this.RequiredApi = item.RequiredApi;
        this.Changes = item.Changes;
        this.Author = item.Author;
        this.AuthorContacts = item.AuthorContacts;
        this.Status = LastCompatibleRepositoryItemStatus.Available;
        this.Url = item.Url;
    }
}