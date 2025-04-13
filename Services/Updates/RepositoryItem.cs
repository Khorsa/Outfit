using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutfitTool.Services.Updates
{
    internal class RepositoryItem
    {
        public string Name = "";
        public Version Version = new Version(0, 0);
        public string Changes = "";
        public Version Require = new Version(0, 0);

        public override string ToString()
        {
            return Name + " " + Version.ToString();
        }
    }
}
