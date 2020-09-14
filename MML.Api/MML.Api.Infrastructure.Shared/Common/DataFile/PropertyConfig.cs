using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Common.DataFile
{
    public class PropertyConfig
    {
        public string PropertyName { get; set; }
        public string HeaderName { get; set; }
        public string NumberFormat { get; set; }

        public string HeaderProperty { get; set; }

        public IList<PropertyConfig> ListProperties { get; set; }
        
    }
}
