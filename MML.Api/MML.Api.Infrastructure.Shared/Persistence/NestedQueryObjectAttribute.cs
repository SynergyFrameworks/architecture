using System;

namespace MML.Enterprise.Persistence
{
    public class NestedQueryObjectAttribute : Attribute
    {
        private Type NestedObjectType { get; set; }
        private string NameAlias { get; set; }

        /// <summary>
        /// This Attribute should be added to any property used to catch a json string from a SQL Query.
        /// The required information is used to facilitate sorting and filtering.
        /// </summary>
        /// <param name="nestedObjectType">This should be the Type of the collection the json will deserialize into.</param>
        /// <param name="nameAlias">This should be the name of the collection property used to access the deserialized collection.</param>
        public NestedQueryObjectAttribute(Type nestedObjectType, string nameAlias)
        {
            NestedObjectType = nestedObjectType;
            NameAlias = nameAlias;
        }

        public Type GetNestedObjectType()
        {
            return NestedObjectType;
        }

        public string GetNameAlias()
        {
            return NameAlias;
        }
    }
}
