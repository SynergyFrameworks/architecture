using MML.Enterprise.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MML.Enterprise.Persistence.Extensions
{
    public static class ObjectExtension
    {
        public static void UpdateEditableFields<T>(this T updatee, object updater, List<string> propertiesToSkip = null, bool mapNulls = false)
        {
            if (propertiesToSkip == null)
            {
                propertiesToSkip = new List<string>();
            }

            var updaterProperties = updater.GetType().GetProperties();
            foreach (var property in updaterProperties)
            {
                var attrs = property.GetCustomAttributes(false);
                if (attrs.Any(attr => string.Equals(attr.GetType().Name, "NonEditableAttribute", StringComparison.Ordinal)))
                {
                    propertiesToSkip.Add(property.Name);
                }
            }
            var method = typeof(ObjectExtensions).GetMethod("UpdateObject");
            method = method.MakeGenericMethod(updatee.GetType());
            method.Invoke(null, new object[] { updatee, updater, propertiesToSkip, Type.Missing, Type.Missing, mapNulls });
        }
    }
}
