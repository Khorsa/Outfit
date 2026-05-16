/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System.Windows.Media.Imaging;

namespace OutfitTool.API1.Dto;

public class CommandContextMenu(string text, BitmapImage? image)
{
    public string Text { get; set; } = text;
    public BitmapImage? Image { get; set; } = image;
}