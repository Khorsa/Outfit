using OutfitTool.Common;

namespace OutfitTool.ModuleManager
{
    public class Module
    {
        public ModuleInfoInterface moduleInfo { get; }
        public ModuleControllerInterface moduleController { get; }
        public string assemblyName { get; }

        public bool enabled { get; set; } = false;

        public Module(
            string assemblyName,
            ModuleInfoInterface moduleInfo,
            ModuleControllerInterface moduleController
            )
        {
            this.moduleInfo = moduleInfo;
            this.moduleController = moduleController;
            this.assemblyName = assemblyName;
        }
    }
}
