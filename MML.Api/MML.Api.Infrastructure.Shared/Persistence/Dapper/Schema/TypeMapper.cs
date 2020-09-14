using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PFASolutions.FirmView.Query.Mapping
{
    public static class TypeMapper
    {
        public static void Initialize(string @namespace)
        {
            IEnumerable<Type> types = from assem in AppDomain.CurrentDomain.GetAssemblies().ToList()
                                      from type in assem.GetTypes()
                                      where type.IsClass && type.Namespace == @namespace
                                      select type;

            types.ToList().ForEach(type =>
            {
                SqlMapper.ITypeMap mapper = (SqlMapper.ITypeMap)Activator
                    .CreateInstance(typeof(ColumnAttributeTypeMapper<>)
                        .MakeGenericType(type));
                SqlMapper.SetTypeMap(type, mapper);
            });
        }
    }
}
