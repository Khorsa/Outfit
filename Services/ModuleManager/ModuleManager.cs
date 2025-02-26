using Common.Logger;
using OutfitTool.Common;
using OutfitTool.Services.HotkeyManager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OutfitTool.ModuleManager
{
    class ModuleManager : ModuleManagerInterface
    {
        private LoggerInterface logger;

        private const string ModulesDirectory = "modules";
        private List<Module> Modules = new List<Module>();

        public ModuleManager(LoggerInterface logger)
        {
            this.logger = logger;
        }

        public void LoadModules(List<string>? enabledModules = null)
        {
            if (enabledModules == null)
            {
                enabledModules = new List<string>();
            }

            // Останавливаем все модули
            foreach (Module module in this.Modules)
            {
                module.moduleController.shutdown();
            }

            // Обнуляем список
            this.Modules.Clear();

            // Подгружаем список установленных модулей
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string modulesPath = Path.Combine(currentDirectory, ModulesDirectory);

            if (Directory.Exists(modulesPath))
            {
                var modulesDirs = Directory.GetDirectories(modulesPath);

                foreach (var moduleDir in modulesDirs)
                {
                    try
                    {
                        string moduleName = Path.GetFileName(moduleDir);

                        var dllFile = Path.Combine(moduleDir, moduleName + ".dll");

                        Assembly assembly = Assembly.LoadFrom(dllFile);
                        Module module = this.parseModule(assembly);
                        module.enabled = enabledModules.Contains(module.assemblyName);

                        this.Modules.Add(module);
                        this.logger.Info("Module " + module.assemblyName + " loaded");
                    }
                    catch (Exception exception)
                    {
                        this.logger.Debug(exception.ToString());
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(modulesPath);
            }
        }

        private Module parseModule(Assembly assembly)
        {
            var types = assembly.GetTypes();

            string? name = assembly.GetName().Name;
            ModuleInfoInterface? info = null;
            ModuleControllerInterface? controller = null;

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAssignableTo(typeof(ModuleInfoInterface)))
                {
                    info = Activator.CreateInstance(type) as ModuleInfoInterface;
                }
                if (type.IsAssignableTo(typeof(ModuleControllerInterface)))
                {
                    controller = Activator.CreateInstance(type) as ModuleControllerInterface;
                }
            }

            if (name == null)
            {
                throw new Exception("Module name can't be resolved");
            }

            if (info == null)
            {
                throw new Exception("ModuleInfoInterface implementation not found in module " + name);
            }

            if (controller == null)
            {
                throw new Exception("ModuleControllerInterface implementation not found in module " + name);
            }

            return new Module(name, info, controller);
        }

        public List<Module> getModules()
        {
            return this.Modules;
        }
        public Module? getModule(string name)
        {
            foreach (Module module in this.Modules)
            {
                if (module.moduleInfo.Name == name)
                {
                    return module;
                }
            }
            return null;
        }

        public Module? GetModule(string name)
        {
            foreach (Module module in this.Modules)
            {
                if (module.moduleInfo.Name == name)
                {
                    return (Module)module;
                }
            }
            return null;
        }

        public CommandDescriptor? GetCommandDescriptor(string commandString)
        {
            string[] a = CommandDescriptor.getModuleAndCommandNames(commandString);

            Module? module = this.GetModule(a[0]);
            CommandInterface? command = null;
            if (module != null)
            {
                foreach (CommandInterface c in module.moduleController.getCommandList())
                {
                    if (c.Name == a[1])
                    {
                        command = c;
                        break;
                    }
                }
            }

            if (module != null && command != null)
            {
                return new CommandDescriptor(module, command);
            }

            return null;
        }
    }
}
