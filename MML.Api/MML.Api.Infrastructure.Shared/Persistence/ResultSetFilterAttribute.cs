using System;
using System.Collections.Generic;
using System.Linq;

namespace MML.Enterprise.Persistence
{
    public class ResultSetFilterAttribute : Attribute
    {
        private Dictionary<string, KeyValuePair<string, string>> MappingOverrides { get; set; }

        /// <summary>
        /// This attribute should be applied to simple get/set properties to indicate their value is used to filter a result set.
        /// All collections here need to match in length and order!
        /// </summary>
        /// <param name="resultSetValues">The values you expect to find in the result of the query.</param>
        /// <param name="keys">The values you would like to display.</param>
        /// <param name="values">The values the query will filter agaist.</param>
        public ResultSetFilterAttribute(string[] resultSetValues, string[] keys, string[] values)
        {
            if (resultSetValues.Length != keys.Length || resultSetValues.Length != values.Length)
                throw new Exception("Invalid constructor arguments for ResultSetFilterAttribute.  All arrays need to match in length.");

            var overrides = new Dictionary<string, KeyValuePair<string, string>>();
            for(var i = 0; i < keys.Length; i++)
            {
                overrides.Add(resultSetValues[i], new KeyValuePair<string, string>(keys[i], values[i]));
            }

            MappingOverrides = overrides;
        }

        /// <summary>
        /// This attribute should be applied to simple get/set properties to indicate their value is used to filter a result set.
        /// </summary>
        public ResultSetFilterAttribute() { MappingOverrides = null; }

        public Dictionary<string, KeyValuePair<string, string>> GetMappingOverrides()
        {
            return MappingOverrides ?? new Dictionary<string, KeyValuePair<string, string>>();
        }

        public Dictionary<string,List<string>> GetReverseMappings()
        {
            return MappingOverrides != null
                ? MappingOverrides.Select(m => new { Key = m.Value.Value, Value = m.Key }).GroupBy(k => k.Key).ToDictionary(o => o.Key, o => o.Select(i => i.Value).ToList())
                : new Dictionary<string,List<string>>();
        }
    }
}
