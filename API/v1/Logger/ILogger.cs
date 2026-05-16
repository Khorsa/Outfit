/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
namespace OutfitTool.API1.Logger;

public interface ILogger
{
    void Debug(string log);
    void Info(string log);
    void Error(string log);
    void Warning(string log);
    void Critical(string log);
    void Status(string log);
}