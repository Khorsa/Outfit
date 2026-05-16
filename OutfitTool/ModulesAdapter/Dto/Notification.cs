/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
namespace OutfitTool.ModulesAdapter.Dto;

public class Notification(string title, string message, string href)
{
    public readonly string Message = message;
    public readonly string Title = title;
    public readonly string Href = href;
}