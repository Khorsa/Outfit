using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OutfitTool.Services.Updates.UpdatesManager;

namespace OutfitTool.Services.Updates
{
    interface UpdatesManagerInterface
    {
        public void LoadList(LoadListHandler updateManagerCheckStatusChanged);
        public void UpdateAll();
        public void Download(RepositoryItem item);
        public void DeleteModule(RepositoryItem module);
    }
}
