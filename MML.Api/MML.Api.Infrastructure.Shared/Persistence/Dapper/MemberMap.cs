using System;
using System.Reflection;
using Dapper;

namespace PFASolutions.FirmView.Query.Mapping
{
    internal class MemberMap : SqlMapper.IMemberMap
    {
        private readonly MemberInfo _member;

        public MemberMap(MemberInfo member, string columnName)
        {
            this._member = member;
            this.ColumnName = columnName;
        }
        public string ColumnName { get; }
        public FieldInfo Field => _member as FieldInfo;

        public Type MemberType
        {
            get
            {
                switch (_member.MemberType)
                {
                    case MemberTypes.Field: return ((FieldInfo)_member).FieldType;
                    case MemberTypes.Property: return ((PropertyInfo)_member).PropertyType;
                    default: throw new NotSupportedException();
                }
            }
        }
        public ParameterInfo Parameter => null;
        public PropertyInfo Property => _member as PropertyInfo;
    }
}