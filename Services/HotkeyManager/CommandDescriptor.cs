using OutfitTool.Common;
using OutfitTool.ModuleManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace OutfitTool.Services.HotkeyManager
{
    public class CommandDescriptor
    {
        public Module module;
        public CommandInterface command;

        public CommandDescriptor(
            Module module,
            CommandInterface command
            )
        {
            this.module = module;
            this.command = command;
        }

        public override string ToString()
        {
            return module.assemblyName + "." + command.Name;
        }

        public static string[] getModuleAndCommandNames(string name)
        {
            return name.Split('.');
        }
    }
}
