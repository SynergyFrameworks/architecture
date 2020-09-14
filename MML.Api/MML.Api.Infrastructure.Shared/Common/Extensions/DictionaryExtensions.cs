using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Common.Extensions
{
    public static class DictionaryExtensions
    {
        public static T ToObject<T>(this IDictionary<string, object> dictionary)
        {
            var obj = Activator.CreateInstance<T>();
            var properties =
                obj.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite && p.CanRead);


            foreach (var item in dictionary)
            {
                try
                {
                    var prop = properties.FirstOrDefault(p => p.Name == item.Key);
                    if (prop == null)
                        continue;
                    prop.SetValue(obj,
                        prop.PropertyType == item.Value.GetType()
                            ? item.Value
                            : Convert.ChangeType(item.Value, prop.PropertyType));
                }
                catch (Exception ex)
                {
                    
                }
            }
            return obj;
        }
    }
}
