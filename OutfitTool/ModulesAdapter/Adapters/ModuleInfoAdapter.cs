/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.API1;
using OutfitTool.ModulesAdapter.Adapters.v1;

namespace OutfitTool.ModulesAdapter.Adapters;

public class ModuleInfoAdapter(Type type)
{
    public IModuleInfo? Get()
    {
        API1.IModuleInfo? info = null;
        if (type.IsAssignableTo(typeof(API1.IModuleInfo)))
        {
            info = Activator.CreateInstance(type) as API1.IModuleInfo;
        }

        if (info != null) return new ModuleInfoV1(info);

        // Проверяем другие версии API...
        return null;

    }
}