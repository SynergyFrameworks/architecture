using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using MML.Enterprise.Persistence.Azure.Extensions;

namespace MML.Enterprise.Persistence.Azure.Transformers
{
    public class JsonCollectionTransformer: IAzureTransformer
    {
        public DynamicPersistentEntity TransformToAzureObject(PersistentEntity obj)
        {
            dynamic entity = new DynamicPersistentEntity();
            var properties = obj.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            entity.PartitionKey = obj.PartitionKey;
            entity.RowKey = obj.Id;            

            foreach (var property in properties)
            {
                var list = property.GetValue(obj) as IList;
                if (list == null)
                {
                    entity[property.Name] = property.GetValue(obj);
                }
                else
                {
                    entity[property.Name]= CollectionToJSONString(list);
                }
            }
            return entity;
        }
        public T TransformFromAzureObject<T>(DynamicPersistentEntity persistantEntity)
        {
            var obj = Activator.CreateInstance<T>();
            var properties = obj.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var value = ((EntityProperty)persistantEntity[property.Name]).GetValue();
                if(value == null || string.IsNullOrEmpty(value.ToString())) continue;

                property.SetValue(obj,
                                  property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof (IList<>)
                                      ? JSONStringToCollection(property.PropertyType,(string)value)
                                      : value);
            }
            return obj;
        }

        public string CollectionToJSONString(IList list)
        {
            return JsonConvert.SerializeObject(list);
        }
        public object JSONStringToCollection(Type T,string value)
        {                        
            var method =
                typeof (JsonConvert).GetMethods()
                                    .FirstOrDefault(
                                        m =>
                                        m.Name == "DeserializeObject" && m.IsGenericMethod &&
                                        m.GetParameters().Count() == 1);
            return method.MakeGenericMethod(T).Invoke(value,new object[] {value});
        }
    }
}
