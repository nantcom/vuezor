using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.Blazor.Vuezor
{

    public struct WatchType
    {
        public bool deep { get; set; }
        public string toWatch { get; set; }

        public WatchType(string toWatch, bool deep = false)
        {
            this.deep = deep;
            this.toWatch = toWatch;
        }
    }

}
