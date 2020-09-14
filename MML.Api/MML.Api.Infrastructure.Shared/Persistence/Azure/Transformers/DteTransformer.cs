//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//using Microsoft.WindowsAzure.Storage.Table;

//using NPOI.SS.Formula.Functions;
//using MML.Enterprise.Common.Extensions;
//using MML.Enterprise.Persistence.Azure.Extensions;

//namespace MML.Enterprise.Persistence.Azure.Transformers
//{
//    public class DteTransformer
//    {
//        private ILog Log = LogManager.GetLogger<DteTransformer>();
//        public int CompressJson { get; set; }

//        public DynamicTableEntity TransformToAzureObject(PersistentEntity obj)
//        {
//            dynamic entity = new DynamicTableEntity();
//            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
//            entity.PartitionKey = obj.PartitionKey;
//            entity.RowKey = obj.RowKey;
//            entity["LastModifiedDate"] = DateTime.UtcNow.GetEntityProperty("LastModifiedDate", CompressJson);

//            var userName = Thread.CurrentPrincipal.Identity.Name;

//            foreach (var property in properties)
//            {
//                var attrs = property.GetCustomAttributes(false);
//                if (!attrs.Any(attr => String.Equals(attr.GetType().Name, "IgnoreProperty", StringComparison.Ordinal)) && property.GetSetMethod() != null)
//                {
//                    var value = property.GetValue(obj);
//                    entity[property.Name] = value.GetEntityProperty(property.Name, CompressJson);
//                    //DateTime.MinValue causes 400...if we have an uninitialized date set it to earliest ATS can handle.
//                    if (value is DateTime && value.Equals(DateTime.MinValue))
//                        entity[property.Name] = new DateTime(1601, 1, 1, 0, 0, 0).GetEntityProperty(property.Name, CompressJson);
//                }
//            }

//            entity["LastModifiedBy"] = userName.GetEntityProperty("LastModifiedBy", CompressJson);

//            try
//            {
//                var createdDate = properties.First(p => String.Equals(p.Name, "CreatedDate", StringComparison.Ordinal));
//                if (createdDate.GetValue(obj).Equals(DateTime.MinValue))
//                    entity[createdDate.Name] = DateTime.UtcNow.GetEntityProperty(createdDate.Name, CompressJson);

//                var createdBy = properties.First(p => String.Equals(p.Name, "CreatedBy", StringComparison.Ordinal));
//                if (createdBy.GetValue(obj) == null || string.IsNullOrEmpty(createdBy.GetValue(obj).ToString()))
//                    entity[createdBy.Name] = userName.GetEntityProperty(createdBy.Name, CompressJson);
//            }
//            catch (Exception e)
//            {
//                Log.ErrorFormat(e.Message, e);
//            }
//            return entity;
//        }

//        public T TransformFromAzureObject<T>(DynamicTableEntity persistantEntity, bool preserveDateTimeKind)
//        {
//            var obj = Activator.CreateInstance<T>();
//            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
//            var sw = new Stopwatch();
//            foreach (var property in properties)
//            {
//                sw.Start();                
//                if (!persistantEntity.Properties.ContainsKey(property.Name)) continue;
//                var attrs = property.GetCustomAttributes(false);
//                if (attrs.Any(attr => String.Equals(attr.GetType().Name, "IgnoreProperty", StringComparison.Ordinal)))
//                    continue;

//                var value = ((EntityProperty)persistantEntity[property.Name]).GetValue();
//                if (value == null || string.IsNullOrEmpty(value.ToString())) continue;

//                var type = property.PropertyType;

//                //special handling for DateTimeKind
//                if (!preserveDateTimeKind &&
//                    (type == typeof (DateTime) || type == typeof (DateTime?)))
//                {
//                    property.SetValue(obj, DateTime.SpecifyKind((DateTime) value, DateTimeKind.Unspecified));
//                    continue;
//                }

//                if (type == typeof (Guid))
//                {
//                    try
//                    {
//                        property.SetValue(obj, Guid.Parse(value.ToString()));
//                        continue;
//                    }
//                    catch (Exception ex)
//                    {
//                        Log.ErrorFormat("Unable to parse Guid from value " + value + "\n" + ex);
//                        throw;
//                    }
//                }

//                if (type.IsEnum)
//                {
//                    try
//                    {
//                        property.SetValue(obj, Enum.Parse(type, value.ToString()));
//                        continue;
//                    }
//                    catch (ArgumentException ex)
//                    {
//                        if (ex.Message == "Requested value '" + value + "' was not found.")
//                        {
//                            property.SetValue(obj, Enum.Parse(type, Enum.GetNames(type)[0]));
//                            continue;
//                        }
//                        Log.ErrorFormat(ex.ToString());
//                        throw;
//                    }
//                    catch (Exception exception)
//                    {
//                        Log.ErrorFormat(exception.ToString());
//                        throw;
//                    }
//                }

//                //special handling for conversion back to decimal...ATS does not support decimals.
//                if (type == typeof(decimal) || type == typeof(decimal?))
//                {
//                    decimal decimalValue;
//                    try
//                    {
//                        decimalValue = Convert.ToDecimal(value);
//                    }
//                    catch (OverflowException)
//                    {
//                        try
//                        {
//                            decimalValue = Convert.ToDecimal(Math.Round((double)value, 4));
//                        }
//                        catch (OverflowException)
//                        {
//                            decimalValue = Convert.ToDecimal(Math.Round((double)value, 2));
//                        }
//                    }
//                    property.SetValue(obj, decimalValue);
//                    continue;
//                }

//                if (type != typeof(string) && type.GetInterfaces().Contains(typeof(IEnumerable)))
//                {
//                    if (value is byte[])
//                    {
//                        using (var stream = new MemoryStream((byte[]) value))
//                        {
//                            value = Encoding.Unicode.GetString(stream.Decompress().GetArray());
//                        }
//                    }
//                    if (type.GetInterfaces().Contains(typeof(IDictionary)))
//                    {
//                        var dict = typeof(Dictionary<,>);
//                        var args = type.GetGenericArguments();
//                        if (args.Count() < 2)
//                            continue;
//                        var constructedDict = dict.MakeGenericType(args);
//                        property.SetValue(obj, Newtonsoft.Json.JsonConvert.DeserializeObject(value.ToString(), constructedDict));
//                        continue;
//                    }
//                    var list = typeof(List<>);
//                    var constructedList = list.MakeGenericType(type.GetGenericArguments());
//                    property.SetValue(obj, Newtonsoft.Json.JsonConvert.DeserializeObject(value.ToString(), constructedList));
//                    continue;
//                }

//                if (!(type.IsPrimitive || type.IsValueType || type == typeof(string) || type == typeof(Decimal) || type == typeof(DateTime)))
//                {
//                    if (value is byte[])
//                    {
//                        using (var stream = new MemoryStream((byte[])value))
//                        {
//                            value = Encoding.Unicode.GetString(stream.Decompress().GetArray());
//                        }
//                    }
//                    property.SetValue(obj, Newtonsoft.Json.JsonConvert.DeserializeObject(value.ToString(), type));
//                    continue;
//                }

//                //handling of bool
//                /*
//                if (type == typeof (Boolean) || type == typeof (Boolean?))
//                {
//                    bool booleanValue;
//                    try
//                    {
//                        booleanValue = Convert.ToBoolean(value);
//                    }
//                    catch (Exception)
//                    {

//                        booleanValue = false;
//                    }
//                    property.SetValue(obj, booleanValue);
//                    continue;
//                }
//                 */
//                sw.Stop();                
//                property.SetValue(obj, value);
//            }
//            //PartitionKey and RowKey are marked as IgnoreProperties becaue they are used as Partition Key and Row Key, need to add them back in manually.
//            if (obj as PersistentEntity != null)
//            {
//                (obj as PersistentEntity).PartitionKey = persistantEntity.PartitionKey;
//                (obj as PersistentEntity).RowKey = persistantEntity.RowKey;
//            }
//            return obj;
//        }
//    }
//}
