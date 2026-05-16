/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.Services.Updates;

namespace OutfitTool.View;

class ListViewRepositoryItem
{
    public string Name;
    public string Versions;

    public ListViewRepositoryItem(string name, string versions)
    {
        this.Name = name;
        this.Versions = versions;
    }
    public static List<ListViewRepositoryItem> MapRepository(List<RepositoryItem> repositoryItem)
    {
        var versions = new Dictionary<string, List<string>>();
        foreach (RepositoryItem item in repositoryItem)
        {
            if (! versions.ContainsKey(item.Name))
            {
                versions.Add(item.Name, new List<string>());
            }
            versions[item.Name].Add(item.Version.ToString());
        }

        var list = new List<ListViewRepositoryItem>();
        foreach (var version in versions) { 
            list.Add(new ListViewRepositoryItem(version.Key, string.Join(", ", version.Value)));
        }

        return list;
    }
}