/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
namespace OutfitTool.View;

public enum LastCompatibleRepositoryItemStatus
{
    Installed = 0,
    Downloading = 1,
    Downloaded = 2,
    Available = 3,
    NotAvailable = 4,
}