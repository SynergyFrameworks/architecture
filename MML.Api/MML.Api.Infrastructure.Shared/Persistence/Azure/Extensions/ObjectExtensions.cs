using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using MML.Enterprise.Common.Extensions;

namespace MML.Enterprise.Persistence.Azure.Extensions
{
    public static class ObjectExtensions
    {
        public static EntityProperty GetEntityProperty(this object value, string key, int compress = 30000)
        {
            if (value == null) return new EntityProperty((string)null);
            if (value.GetType() == typeof(byte[])) return new EntityProperty((byte[])value);
            if (value is bool) return new EntityProperty((bool)value);
            if (value is DateTimeOffset) return new EntityProperty((DateTimeOffset)value);
            if (value is DateTime) return new EntityProperty((DateTime)value);
            if (value is double) return new EntityProperty((double)value);
            if (value is Guid) return new EntityProperty((Guid)value);
            if (value is int) return new EntityProperty((int)value);
            if (value is long) return new EntityProperty((long)value);
            if (value is string) return new EntityProperty((string)value);
            if (value is decimal) return new EntityProperty(Convert.ToDouble(value));
            if (value is IEnumerable || !value.IsBuiltinType()) return JsonEntity(value,compress);
            if (value is Enum) return new EntityProperty(Enum.GetName(value.GetType(), value));
            //throw new Exception(string.Format("EntityProperty for Key {0} of type {1} is not supported.", key, value.GetType()));
            return new EntityProperty((string)null); //By default return null - any special handling will be done by the transformers themselves.
        }

        private static EntityProperty JsonEntity(object value, int compress)
        {
            var jsonString = JsonConvert.SerializeObject(value);
            if (compress != null && value.ToString().Length > compress)
            {
                using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    return new EntityProperty(stream.Compress().GetArray());
                }
            }
            return new EntityProperty(jsonString);
        }
    }
}
