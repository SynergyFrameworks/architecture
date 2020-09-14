using System;
using System.Collections.Generic;
using System.Linq;

namespace MML.Enterprise.Common.Mapping
{
    public class PropertyMapping
    {
        public string PropertyName { get; set; }
        public string MappingInfo { get; set; }
        public string HeaderName { get; set; }
        public bool IsHeaderMapping { get; set; }
        public int RowNumber
        {
            get
            {
                if (string.IsNullOrEmpty(MappingInfo)) return 0;
                var parts = MappingInfo.Split('!');
                if (parts.Length < 2 || string.IsNullOrEmpty(parts[1]))
                    return 0;                
                try
                {
                    
                    return int.Parse(new string(parts[1].Where(Char.IsDigit).ToArray()));
                }
                catch(Exception ex)
                {
                    return 0;
                }                
            }
        }

        public string Column
        {
            get
            {
                if (string.IsNullOrEmpty(MappingInfo)) return "A";
                var parts = MappingInfo.Split('!');
                if (parts.Length < 2 || string.IsNullOrEmpty(parts[1]))
                    return "A";
                try
                {
                    var digits = new [] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
                    var index = parts[1].IndexOfAny(digits);
                    return index > 0 ? parts[1].Substring(0, index) : parts[1];
                }
                catch (Exception ex)
                {
                    return "A";
                }    
            }
        }

        public string SheetName
        {
            get
            {
                if (string.IsNullOrEmpty(MappingInfo)) return null;
                var parts = MappingInfo.Split('!');
                return parts[0];
            }
        }

        public string Format { get; set; }

        public IList<PropertyMapping> PropertyMappings { get; set; }

        public string PropertyPrefix { get; set; } //Such as Summary, Data 
    }
}
