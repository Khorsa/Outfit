using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutfitTool.View
{
    class StatusBarEventArgs: EventArgs
    {
        public string Info;
        public StatusBarEventArgs(string Info) 
        { 
            this.Info = Info;
        }
    }
}
