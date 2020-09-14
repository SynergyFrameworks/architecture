using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MML.Enterprise.Common.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// source : http://stackoverflow.com/questions/155303/net-how-can-you-split-a-caps-delimited-string-into-an-array
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Humanize(this string s)
        {
            return string.IsNullOrEmpty(s) ? null : Regex.Replace(s, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ");
        }

        public static string ToTitleCase(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            var ci = Thread.CurrentThread.CurrentCulture;
            var ti = ci.TextInfo;

            return ti.ToTitleCase(s.ToLower(ci));
        }

        public static string FirstLetterToUpper(this string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }

        public static string FirstLetterToLower(this string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToLower(str[0]) + str.Substring(1);

            return str.ToLower();
        }

        public static string ExceptBlanks(this string str)
        {
            StringBuilder sb = new StringBuilder(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (!char.IsWhiteSpace(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        public static string CollapseWhiteSpace(this string str)
        {
            return Regex.Replace(str, @"\s+", " ");
        }

        public static string ExceptChars(this string str, IEnumerable<char> toExclude)
        {
            StringBuilder sb = new StringBuilder(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (!toExclude.Contains(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Find the first index of an exact string using regex.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="searchText"></param>
        /// <param name="ignoreCase">Defaults to false.</param>
        /// <returns></returns>
        public static int IndexOfExact(this string str, string searchText, bool ignoreCase = false, bool reverse = false)
        {
            var searchString = string.Format(@"\b{0}\b", Regex.Escape(searchText));
            return reverse ? str.RegexIndexOf(searchString, ignoreCase, RegexOptions.RightToLeft) : str.RegexIndexOf(searchString, ignoreCase);
        }

        /// <summary>
        /// Find the first index of an exact string using regex.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="searchText"></param>
        /// <param name="startIndex"></param>
        /// <param name="ignoreCase">Defaults to false.</param>
        /// <returns></returns>
        public static int IndexOfExact(this string str, string searchText, int startIndex, bool ignoreCase = false, bool reverse = false)
        {
            if (startIndex < 1)
                return str.IndexOfExact(searchText, ignoreCase, reverse);

            var subString = str.Substring(startIndex);
            var subStringIndex = IndexOfExact(subString, searchText, ignoreCase, reverse);
            return subStringIndex == -1 ? subStringIndex : subStringIndex + startIndex;
        }

        public static int RegexIndexOf(this string str, string pattern, RegexOptions options = RegexOptions.None)
        {
            var m = Regex.Match(str, pattern, options);
            return m.Success ? m.Index : -1;
        }

        public static int RegexIndexOf(this string str, string pattern, bool ignoreCase, RegexOptions options = RegexOptions.None)
        {
            var strCopy = str; // Regex.Replace(str, @"\t|\r|\n","  ");
            var patternCopy = pattern;
            if (ignoreCase)
            {
                strCopy = strCopy.ToUpper();
                patternCopy = pattern.ToUpper();
            }
            var m = Regex.Match(str, pattern, options);
            return m.Success ? m.Index : -1;
        }

        public static decimal GetDecimalValue(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            var eIndex = value.IndexOf("E");
            var percent = value.Last() == '%';
            if ((percent))
            {
                var valPart = value.Substring(0, value.Length - 1);
                return Convert.ToDecimal(valPart) / 100;
            }
            if (eIndex == -1)
                return Convert.ToDecimal(value);
            var pos = value.Substring(eIndex + 1, 1) == "+";
            var numPart = value.Substring(0, eIndex);
            var ePart = value.Substring(eIndex + 2);
            return pos ? Convert.ToDecimal(numPart) * Convert.ToDecimal(ePart) : Convert.ToDecimal(numPart) / Convert.ToDecimal(ePart);
        }
    }
}
