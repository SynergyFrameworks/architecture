using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Excel
{
    /// <summary>
    /// This is used when processing the properties of data to import that has "refData" attribute. We store the property name, usually in format like Data.xxx or Summary.xxx
    /// </summary>
    public class ImportReferencedataList
    {
        public string PropertyName { get; set; } //"Data." or "Summary." plus property name of the property that has the attribute
        public Type ReferenceDataType { get; set; }
        public IEnumerable<object> ReferenceData { get; set; }

        public bool HasReferenceData
        {
            get
            {
                return ReferenceData != null && ReferenceData.Count() > 0;
            }
        }
    }
}
