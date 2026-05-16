/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.ModulesAdapter.Adapters.v1;

namespace OutfitTool.ModulesAdapter.Adapters;

public class ModuleControllerAdapter(Type type)
{
    public IModuleController? Get()
    {
        API1.IModuleController? controller = null;
        if (type.IsAssignableTo(typeof(API1.IModuleController)))
        {
            controller = Activator.CreateInstance(type) as API1.IModuleController;
        }
        if (controller != null) return new ModuleControllerV1(controller);
            
        // Проверяем другие версии API...
        return null;
    }
}