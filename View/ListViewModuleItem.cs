using OutfitTool.ModuleManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutfitTool.View
{
    internal class ListViewModuleItem
    {
        public ListViewModuleItem(Module module)
        {
            this.module = module;
            this.name = module.moduleInfo.Name;
            this.state = module.enabled ? LocalizationHelper.getString("On") : LocalizationHelper.getString("Off");
        }

        public Module module { get; set; }
        public string name { get; set; }
        public string state { get; set; }
    }
}
