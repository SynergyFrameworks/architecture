using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;

namespace MML.Enterprise.Common.Extensions
{
    public static class ObjectExtensions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ObjectExtensions));
        public static IDictionary<string, object> ToDictionary(this Object obj,
            BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance)
        {         
            var properties = obj.GetType().GetProperties(bindingAttr);
            var response = new Dictionary<string, object>();
            foreach (var prop in properties)
            {
                var value = obj.GetPropertyValue(prop.Name);
                if (value.IsBuiltinType())
                    response.Add(prop.Name, value);
                else if (value is IEnumerable)
                    response.Add(prop.Name, ((IEnumerable<object>)value).Select(o => o.ToDictionary()).ToList());
                else
                {
                    response.Add(prop.Name, value.ToDictionary());
                }
            }
            return response;
        }

        public static IDictionary<string, object> ToDictionary(this Object obj, List<string> properties,
            BindingFlags bindingAttr =  BindingFlags.Public | BindingFlags.Instance)
        {
            var response = new Dictionary<string, object>();
            var props = obj.GetType().GetProperties(bindingAttr).Where(p => properties.Contains(p.Name));
            foreach (var property in properties)
            {
                var propInfo = props.FirstOrDefault(p => p.Name == property);
                if (propInfo == null) continue;
                var value = obj.GetPropertyValue(property);
                if (value.IsBuiltinType())
                    response.Add(property,value);
                else if (value is IEnumerable)
                    response.Add(property,((IEnumerable<object>) value).Select(o => o.ToDictionary()).ToList());
                else
                {
                    response.Add(property,value.ToDictionary());
                }
            }
            return response;
        }

        //TODO fix this to handle setting nested properties
        public static void SetProperty(this object instance, string name, object value)
        {
            var propertyInfo = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            propertyInfo.SetValue(instance, value);
        }

        
        public static object GetPropertyValue(this Object obj, string propertyName)
        {
            if (obj == null)
                return null;
            if (propertyName.Contains("."))
            {
                var dotNotation = propertyName.Split('.');


                return
                    obj.GetType()
                       .GetProperty(dotNotation[0])
                       .GetValue(obj, null)
                       .GetPropertyValue(propertyName.Substring(propertyName.IndexOf('.') + 1));
            }

            var propertyInfo = obj.GetType().GetProperty(propertyName);

            return propertyInfo != null ? propertyInfo.GetValue(obj, null) : null;
        }

        public static T Convert<T>(this Object obj, IList<string> propertiesToSkip = null, IList<Exception> exceptions = null)
        {
            if (obj.GetType() == typeof (T))
                return (T)obj;

            return PopulateNewObject<T>(obj, propertiesToSkip, exceptions); //newObj;
        }

        public static T BlankIfNull<T>(this T instance)  where T : class, new()
        {
            if (instance == null)
                return new T();
            return instance;
        }

        public static string SafeToString(this object instance, string defaultMessage = null)
        {
            return instance == null ? defaultMessage : instance.ToString();
        }

        public static bool IsBuiltinType(this object obj)
        {
            if (obj == null)
                return true;
            var type = obj.GetType();
            return type.IsPrimitive || type.IsValueType || type == typeof(string) || type == typeof(Decimal) || type == typeof(DateTime);            
        }

        public static T Copy<T>(this T obj, IList<string> propertiesToSkip = null, IList<Exception> exceptions = null) where T : class
        {
            var newObj = Activator.CreateInstance<T>();
            if (obj == null)
                return newObj;
            return PopulateNewObject<T>(obj, propertiesToSkip, exceptions);
        }

        private static T PopulateNewObject<T>(Object obj, IList<string> propertiesToSkip = null, IList<Exception> exceptions = null)
        {
            var newObj = Activator.CreateInstance<T>();
            try
            {
                newObj.UpdateObject(obj, propertiesToSkip, false, exceptions);
                return newObj;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error populating new object of type {0} from existing object: {1}",typeof (T),ex);
                if(exceptions != null)
                    exceptions.Add(ex);
            }
            return newObj;
        }

        /// <summary>
        /// This method updates the extension object with the property changes found in the updater
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="updatee"></param>
        /// <param name="updater"></param>
        /// <param name="propertiesToSkip"></param>
        /// <param name="merge"></param>
        /// <param name="exceptions"></param>
        /// <param name="mapNulls"></param>
        public static void UpdateObject<T>(this T updatee, object updater, IList<string> propertiesToSkip = null, bool merge = true, IList<Exception> exceptions = null, bool mapNulls = false)
        {
            if (propertiesToSkip == null) { propertiesToSkip = new List<string>();}
            var updaterProperties = updater.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);
            foreach (var updaterProperty in updaterProperties)
            {
                try
                {
                    var updateeProperty = updatee.GetType().GetProperty(updaterProperty.Name);
                    if (updateeProperty != null && propertiesToSkip.All(p => p != updaterProperty.Name) && updateeProperty.CanWrite)
                    {
                        ConvertProperty(updaterProperty, updateeProperty, updater, updatee, merge, exceptions, mapNulls);
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Error updating object {0} from object {1}: {2}", updatee, updater, ex);
                    if (exceptions != null)
                        exceptions.Add(ex);
                }
            }
        }

        /// <summary>
        /// Maps the value of the updater property on to the updatee property
        /// </summary>
        /// <param name="updaterProperty"></param>
        /// <param name="updateeProperty"></param>
        /// <param name="oldObject"></param>
        /// <param name="newObject"></param>
        /// <param name="merge"></param>
        /// <param name="exceptions"></param>
        /// <param name="mapNulls"></param>
        public static void ConvertProperty(PropertyInfo updaterProperty, PropertyInfo updateeProperty, object oldObject, object newObject, bool merge, IList<Exception> exceptions = null, bool mapNulls = false)
        {
            try
            {
                var value = updaterProperty.GetValue(oldObject);
                if (value != null)
                {
                    if (updateeProperty.PropertyType == typeof (Guid) && value is string)
                    {
                        updateeProperty.SetValue(newObject, Guid.Parse(value.ToString()));
                    }
                    else if (value is Enum && updateeProperty.PropertyType == typeof (string))
                    {
                        updateeProperty.SetValue(newObject, Enum.GetName(value.GetType(), value));
                    }
                    else if (value is string && updateeProperty.PropertyType.BaseType == typeof (Enum))
                    {
                        updateeProperty.SetValue(newObject,
                            Enum.Parse(updaterProperty.PropertyType, value.ToString()));
                    }
                    else if (value.GetType() == updateeProperty.PropertyType)
                    {
                        updateeProperty.SetValue(newObject, value);
                    }

                    else if (updateeProperty.PropertyType.IsGenericType &&
                             updateeProperty.PropertyType.GetGenericTypeDefinition() == typeof (Nullable<>))
                    {
                        var underlyingType = updateeProperty.PropertyType.GetGenericArguments().FirstOrDefault();
                        if (underlyingType == value.GetType())
                            updateeProperty.SetValue(newObject, value);
                    }
                    else if (updateeProperty.PropertyType == typeof (string))
                    {
                        updateeProperty.SetValue(newObject, value.ToString());
                    }
                    else if (updateeProperty.PropertyType.GetInterfaces().Contains(typeof (IEnumerable)))
                    {
                        var type = updateeProperty.PropertyType.GetGenericArguments().FirstOrDefault();
                        if (type == null)
                            updateeProperty.SetValue(newObject, value);
                        else
                        {
                            var firstElement = updateeProperty.PropertyType.GetGenericArguments().FirstOrDefault();
                            var hasId = firstElement != null ? firstElement.GetProperty("Id") : null;
                            if (merge && hasId != null)
                            {
                                var listMethod = typeof(EnumerableExtensions).GetMethod("MergeList");
                                listMethod = listMethod.MakeGenericMethod(type);
                                updateeProperty.SetValue(newObject, listMethod.Invoke(value, new[] { updateeProperty.GetValue(newObject), value }));
                            }
                            else
                            {
                                var listMethod = typeof(EnumerableExtensions).GetMethod("ConvertList");
                                listMethod = listMethod.MakeGenericMethod(type);
                                updateeProperty.SetValue(newObject, listMethod.Invoke(value, new[] { value, null }));
                            }

                        }
                    }
                    else
                    {
                        var method = typeof (ObjectExtensions).GetMethod("Convert");
                        method = method.MakeGenericMethod(updateeProperty.PropertyType);
                        updateeProperty.SetValue(newObject, method.Invoke(value, new[] {value, null, null}));
                    }

                }
                else if (mapNulls)
                {
                    updateeProperty.SetValue(newObject, value);
                }
            }
            catch (Exception ex)
            {
                Log.WarnFormat("Error converting udpaterProperty {0} to updateeProperty {1}: {2}", updaterProperty, updateeProperty, ex);
                if (exceptions != null)
                    exceptions.Add(ex);
            }
        }

        public static DateTime? ToNullableDateTime(this object rangeValue)
        {
            if (rangeValue != null)
                try
                {
                    return DateTime.Parse(rangeValue.ToString());
                }
                catch
                {
                    return null;
                }
            return null;
        }

        public static int? ToNullableInt(this object rangeValue)
        {
            if (rangeValue != null)
                try
                {
                    return System.Convert.ToInt32(rangeValue);
                }
                catch
                {
                    return null;
                }
            return null;
        }

        public static decimal? ToNullableDecimal(this object rangeValue)
        {
            if (rangeValue != null)
                try
                {
                    return System.Convert.ToDecimal(rangeValue);
                }
                catch
                {
                    return null;
                }
            return null;
        }

    }
}
