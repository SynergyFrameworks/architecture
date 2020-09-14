using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using MML.Enterprise.Common.Extensions;
using MML.Enterprise.Persistence.Azure.Extensions;

namespace MML.Enterprise.Persistence.Azure.Transformers
{
    public class BaseTransformer : IAzureTransformer
    {
        public DynamicPersistentEntity TransformToAzureObject(PersistentEntity obj)
        {
            dynamic entity = new DynamicPersistentEntity();
            var properties = obj.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            entity.PartitionKey = obj.PartitionKey;
            entity.RowKey = obj.Id.ToString();
            entity["LastModifiedDate"] = DateTime.UtcNow;

            var userName = Thread.CurrentPrincipal.Identity.Name;
            entity["LastModifiedBy"] = userName;

            foreach (var property in properties)
            {
                var attrs = property.GetCustomAttributes(false);
                if (!attrs.Any(attr => String.Equals(attr.GetType().Name, "IgnoreProperty", StringComparison.Ordinal)))
                {
                    var value = property.GetValue(obj);
                    entity[property.Name] = value;
                    //DateTime.MinValue causes 400...if we have an uninitialized date set it to earliest ATS can handle.
                    if (value is DateTime && value.Equals(DateTime.MinValue))
                        entity[property.Name] = new DateTime(1601, 1, 1, 0, 0, 0);
                }
            }
            
            var createdDate = properties.First(p => String.Equals(p.Name, "CreatedDate", StringComparison.Ordinal));
            if(createdDate.GetValue(obj).Equals(DateTime.MinValue))
                entity[createdDate.Name] = DateTime.UtcNow;

            var createdBy = properties.First(p => String.Equals(p.Name, "CreatedBy", StringComparison.Ordinal));
            if (createdBy.GetValue(obj) == null || string.IsNullOrEmpty(createdBy.GetValue(obj).ToString()))
                entity[createdBy.Name] = userName;
            
            return entity;
        }

        public T TransformFromAzureObject<T>(DynamicPersistentEntity persistantEntity)
        {
            var obj = Activator.CreateInstance<T>();
            var properties = obj.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var attrs = property.GetCustomAttributes(false);
                if(attrs.Any(attr => String.Equals(attr.GetType().Name, "IgnoreProperty", StringComparison.Ordinal)))
                    continue;
                
                var value = ((EntityProperty)persistantEntity[property.Name]).GetValue();
                if (value == null || string.IsNullOrEmpty(value.ToString())) continue;

                //special handling for conversion back to decimal...ATS does not support decimals.
                if (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?))
                {
                    decimal decimalValue;
                    try
                    {
                        decimalValue = Convert.ToDecimal(value);
                    }
                    catch (OverflowException)
                    {
                        try
                        {
                            decimalValue = Convert.ToDecimal(Math.Round((double) value, 4));
                        }
                        catch (OverflowException)
                        {
                            decimalValue = Convert.ToDecimal(Math.Round((double) value, 2));
                        }
                    }
                    property.SetValue(obj,decimalValue);
                    continue;
                }
                property.SetValue(obj,value);
            }
            //PartitionKey and Id are marked as IgnoreProperties becaue they are used as Partition Key and Row Key, need to add them back in manually.
            if (obj as PersistentEntity != null)
            {
                (obj as PersistentEntity).PartitionKey = persistantEntity.PartitionKey;
                (obj as PersistentEntity).Id = new Guid(persistantEntity.RowKey);
            }
            return obj;
        }
    }
}
