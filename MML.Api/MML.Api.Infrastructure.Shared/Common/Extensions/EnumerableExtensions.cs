using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace MML.Enterprise.Common.Extensions
{
    public static class EnumerableExtensions
    {
        public static List<T> ConvertList<T>(this IEnumerable list, IList<string> propertiesToSkip = null)
        {
            var newList = Activator.CreateInstance<List<T>>();
            newList.AddRange(from object item in list select item.Convert<T>(propertiesToSkip));
            return newList;
        }

       
        public static IList<T> MergeList<T>(this IList<T> mergeeList, IEnumerable mergerCollection)
        {
            // Items must have Id values
            // If item is in merger list but not mergee list, add it
            // If item is in both lists, update it and add it.
            if (mergerCollection == null)
                return EmptyList(mergeeList);
            
            var mergerList = mergerCollection as IList<object> ?? mergerCollection.Cast<object>().ToList();

            if (!mergerList.Any())
                return EmptyList(mergeeList);

            var newList = Activator.CreateInstance<List<T>>();
            if (mergeeList == null)
            {
                mergeeList = Activator.CreateInstance<List<T>>();
            }

            var newMergeeList = new Dictionary<string, T>();
            var newMergerList = new List<string>();
            foreach (var destinationItem in mergeeList)
            {
                var idInfo = destinationItem.GetType().GetProperty("Id");
                var id = idInfo != null ? idInfo.GetValue(destinationItem, null) : null;
                var guidId = id as Guid?;
                if(id == null || guidId == Guid.Empty)
                    throw new InvalidDataException("Cannot merge properties without Ids");

                if (!string.IsNullOrEmpty(id.ToString()))
                {
                    newMergeeList.Add(id.ToString(), destinationItem);
                }

            }
            foreach (var originItem in mergerList)
            {
                var idInfo = originItem.GetType().GetProperty("Id");
                var isNewItem = true;
                if (idInfo != null)
                {
                    var id = idInfo.GetValue(originItem, null).BlankIfNull().ToString();
                    if (!string.IsNullOrEmpty(id) && newMergeeList.ContainsKey(id))
                    {
                        newMergerList.Add(id);
                        var oldItem = newMergeeList[id];
                        oldItem.UpdateObject(originItem);
                        isNewItem = false;
                    }
                }
                if (isNewItem)
                {
                    var newItem = (T) Activator.CreateInstance(typeof(T));
                    newItem.UpdateObject(originItem);
                    newList.Add(newItem);
                }

            }
            foreach (var mergeeItem in newMergeeList.Where(mergeeItem => !newMergerList.Contains(mergeeItem.Key)))
            {
                mergeeList.Remove(mergeeItem.Value);
            }
            foreach (var newItem in newList)
            {
                mergeeList.Add(newItem);
            }
            return mergeeList;
        }

        private static IList<T> EmptyList<T>(IList<T> mergeeList)
        {
            if (mergeeList == null)
                return null;
            mergeeList.Clear();
            return mergeeList;
        } 
    }
}
