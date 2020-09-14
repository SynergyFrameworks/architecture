using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace MML.Enterprise.Persistence.Azure.Extensions
{
    public static class EntityPropertyExtensions
    {
        public static object GetValue(this EntityProperty item)
        {
            var propertyType = item.PropertyType;
            if (propertyType == EdmType.String) return item.StringValue;
            if (propertyType == EdmType.DateTime) return item.DateTime;
            if (propertyType == EdmType.Binary) return item.BinaryValue;
            if (propertyType == EdmType.Double) return item.DoubleValue;
            if (propertyType == EdmType.Int32) return item.Int32Value;
            if (propertyType == EdmType.Int64) return item.Int64Value;
            if (propertyType == EdmType.Guid) return item.GuidValue;
            if (propertyType == EdmType.Boolean) return item.BooleanValue;
            return null;
        }
    }
}
