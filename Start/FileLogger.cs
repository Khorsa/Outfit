/*
 * Copyright (C) 2026 Roman Stolyarov <rshome@mail.ru>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Start
{
    public class FileLogger : ILoggerInterface
    {
        private readonly string _fileName;
        public FileLogger() 
        {
            string appDirectory = AppContext.BaseDirectory;
            this._fileName = Path.Combine(AppContext.BaseDirectory, "start.log");
        }

        public void Error(string message)
        {
            this._Log("ERROR", message);
        }

        public void Info(string message)
        {
            this._Log("INFO", message);
        }

        public void Warning(string message)
        {
            this._Log("WARNING", message);
        }

        private void _Log(string level, string message)
        {
            File.AppendAllLines(this._fileName, new List<string> {
                DateTime.Now.ToString() + " "+level+" " + message
            });
        }
    }
}
