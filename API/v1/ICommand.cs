/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using OutfitTool.API1.Dto;

namespace OutfitTool.API1;

public interface ICommand
{
    string Name { get; }
    string Description { get; }
    CommandContextMenu? ContextMenu { get; }
    void Run();
}