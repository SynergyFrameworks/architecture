using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dapper;

namespace PFASolutions.FirmView.Query.Mapping
{
    internal class CustomTypeMap : SqlMapper.ITypeMap
    {
        private readonly Dictionary<string, SqlMapper.IMemberMap> _members
            = new Dictionary<string, SqlMapper.IMemberMap>(StringComparer.OrdinalIgnoreCase);

        private Type Type { get; }
        private readonly SqlMapper.ITypeMap _tail;
        public void Map(string columnName, string memberName)
        {
            _members[columnName] = new MemberMap(Type.GetMember(memberName).Single(), columnName);
        }
        public CustomTypeMap(Type type, SqlMapper.ITypeMap tail)
        {
            this.Type = type;
            this._tail = tail;
        }
        public ConstructorInfo FindConstructor(string[] names, Type[] types)
        {
            return _tail.FindConstructor(names, types);
        }

        public ConstructorInfo FindExplicitConstructor()
        {
            throw new NotImplementedException();
        }

        public SqlMapper.IMemberMap GetConstructorParameter(
            System.Reflection.ConstructorInfo constructor, string columnName)
        {
            return _tail.GetConstructorParameter(constructor, columnName);
        }

        public SqlMapper.IMemberMap GetMember(string columnName)
        {
            if (!_members.TryGetValue(columnName, out var map))
            { // you might want to return null if you prefer not to fallback to the
                // default implementation
                map = _tail.GetMember(columnName);
            }
            return map;
        }
    }
}


//class SomeType
//{
//    public string Bar { get; set; }
//    public int Foo { get; set; }
//}


//// only need to do this ONCE
//var oldMap = SqlMapper.GetTypeMap(typeof(SomeType));
//var map = new CustomTypeMap(typeof(SomeType), oldMap);
//map.Map("IFoo", "Foo");
//map.Map("SBar", "Bar");
//SqlMapper.SetTypeMap(map.Type, map);