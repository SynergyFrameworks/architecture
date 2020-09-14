using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using log4net;

namespace MML.Enterprise.Excel
{
    public static class RangeExtensions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RangeExtensions));
        public static string GetStringValueOrNull(this IRange cell)
        {
            return cell.Value == null ? null : cell.Value.ToString();
        }

        public static bool? GetBoolValueOrNull(this IRange cell)
        {
            if (cell.Value == null)
                return null;
            return bool.Parse(cell.Value.ToString());
        }

        public static DateTime? GetDateTimeValueOrNull(this IRange cell)
        {
            if (cell.Value == null)
                return null;
            return DateTime.Parse(cell.Value.ToString());
        }

        public static T MatchReferenceData<T>(this IRange cell, Dictionary<string, T> data, bool cleanValue = true) where T : class
        {

            if (data == null || !data.Any())
            {
                Log.InfoFormat("No entries in reference collection of {0}", typeof(T).Name);
                return null;
            }
            var key = GetStringValueOrNull(cell);
            if (key == null)
                return null;

            return key.MatchReferenceData(data, cleanValue);
        }

        public static T MatchReferenceData<T>(this string key, Dictionary<string, T> data, bool cleanValue =false) where T : class
        {
            if (key == null)
                return null;
            if (cleanValue)
            {
                key = Regex.Replace(key, @"\s+", " ").Trim();
            }
            if (!data.ContainsKey(key))
            {
                var keyList = data.Keys.Aggregate("", (current, nextKey) => nextKey + ", " + current);
                keyList.Remove(keyList.Length - 2);
                Log.WarnFormat("Unable to match {2} reference value with range key {0}; options are: {1}", key, keyList, typeof(T).Name);
                throw new Exception(string.Format($"Unable to match {typeof(T).Name} reference value with range key {key}; options are: {keyList}"));
            }
            return data[key];
        }
    }
}
