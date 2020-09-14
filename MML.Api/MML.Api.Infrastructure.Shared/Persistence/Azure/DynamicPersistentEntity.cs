using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using MML.Enterprise.Persistence.Azure.Extensions;

namespace MML.Enterprise.Persistence.Azure
{
    public class DynamicPersistentEntity : DynamicObject, ITableEntity
    {
        public IDictionary<string, EntityProperty> Properties { get; set; }

        public DynamicPersistentEntity()
        {
            Properties = new Dictionary<string, EntityProperty>();
        }

        public object this[string key]
        {
            get
            {
                if (!Properties.ContainsKey(key))
                {
                    Properties.Add(key, new EntityProperty((string)null));
                }

                return Properties[key];
            }
            set
            {
                var property = value.GetEntityProperty(key);

                if (!Properties.ContainsKey(key))
                {
                    Properties.Add(key, property);
                }
                else
                {
                    Properties[key] = property;
                }
            }
        }


        #region DynamicObject - Add support of Getting and Setting properties


        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = this[binder.Name];
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this[binder.Name] = value;
            return true;
        }
        #endregion

        #region ITableEntity Implementation
        public string ETag { get; set; }

        public string PartitionKey { get; set; }
        public double TestValue { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return Properties;
        }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            Properties = properties;
        }

        #endregion


    }
}
