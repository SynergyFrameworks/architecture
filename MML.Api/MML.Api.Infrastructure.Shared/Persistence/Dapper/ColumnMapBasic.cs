using System.Collections.Generic;

namespace PFASolutions.FirmView.Query.Mapping
{
    public class ColumnMapBasic
    {
        private readonly Dictionary<string, string> _forward = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _reverse = new Dictionary<string, string>();

        public void Add(string t1, string t2)
        {
            _forward.Add(t1, t2);
            _reverse.Add(t2, t1);
        }

        public string this[string index]
        {
            get
            {
                // Check for a custom column map.
                if (_forward.ContainsKey(index))
                {
                    return _forward[index];
                }

                if (_reverse.ContainsKey(index))
                {
                    return _reverse[index];
                }

                // If no custom mapping exists, return the value passed in.
                return index;
            }
        }
    }

    // Example of Implementation
    //var columnMap = new ColumnMap();
    //columnMap.Add("Field1", "Column1");
    //columnMap.Add("Field2", "Column2");
    //columnMap.Add("Field3", "Column3");

    //SqlMapper.SetTypeMap(typeof (Class), new CustomPropertyTypeMap(typeof (Class), (type, columnName) => type.GetProperty(columnMap[columnName])));

}
