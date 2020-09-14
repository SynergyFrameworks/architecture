using System.Collections.Generic;

namespace MML.Enterprise.Common.Mapping
{
    public class Mapping
    {
        public string ObjectName { get; set; }
        public string ParentName { get; set; }
        public bool SubstituteYesForTrue { get; set; }
        public IList<PropertyMapping> PropertyMappings { get; set; }
    }
}
