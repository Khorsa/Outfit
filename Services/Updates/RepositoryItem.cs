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
        public int MajorVersion = 1;
        public int MinorVersion = 0;

        public override string ToString()
        {
            return Name + " " + MajorVersion + "." + MinorVersion;
        }
    }
}
