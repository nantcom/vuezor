using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.Blazor.Vuezor
{
    public class CustomScript
    {
        public string Url { get; set; }

        /// <summary>
        /// Key in Window object that will be created by script.
        /// Such as Vue which was created as Window[Vue]
        /// </summary>
        public string WatchObjectName { get; set; }
    }
}
