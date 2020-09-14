using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
//using Yuhan.Common.Extensions;

namespace MML.Enterprise.Persistence.Dapper
{
    public class TableMapper
    {
        public static string SplitCamelCase(string str, string tble)
        {

            string strCamel = (Regex.Replace(Regex.Replace(str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1_$2"), @"(\p{Ll})(\P{Ll})", "$1_$2").ToUpper());

            return strCamel.Insert(0, tble + "_");

        }
        public enum AuditType
        {
            FULL, LOG, NONE
        }
        public static Dictionary<string, string> GetTableMapping(object obj, bool isSelect = false, string subName = "")
        {
            if (subName != "")
            {
                subName = subName + "$";
            }

            if (!isSelect)
            {
                subName = "";
            }

            Type myType = obj.GetType();
            IList<dynamic> tableAtrribs = new List<dynamic>(myType.GetCustomAttributes());
            var _dict = new Dictionary<string, string>();
            var tableName = "";
            foreach (var attrib in tableAtrribs)
            {

                if (attrib.GetType().Name == "TableRefAttribute")
                {
                    tableName = attrib.GetType().Name;

                }
                _dict.Add(subName + "Id", attrib.TableIdentifier);
                _dict.Add(subName + "table", attrib.TableName);
                _dict.Add(subName + "tableType", attrib.TableType.ToString());
            }
            IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());
            foreach (PropertyInfo prop in props)
            {
                IList<dynamic> columnAttribs = new List<dynamic>(prop.GetCustomAttributes());
                foreach (var _propAttrib in columnAttribs)
                {

                    if (_propAttrib.TypeId.Name.Trim() == "MapColumnAttribute")
                    {
                        _dict.Add(subName + prop.Name, _propAttrib.ColumnName);
                        if (isSelect && prop.PropertyType.IsClass && prop.PropertyType.Name != "String" && !_propAttrib.SkipSelect)
                        {
                            var subObj = Activator.CreateInstance(prop.PropertyType);
                            var subDict = GetTableMapping(subObj, isSelect: isSelect, subName: prop.PropertyType.Name);
                          //  _dict.AddRange(subDict);
                        }
                    }
                }
            }
      
            return _dict;
        }
    }
}
