using System;

namespace MML.Enterprise.Persistence
{
    public class NonExistantColumnException : Exception
    {
        public NonExistantColumnException() : base() { }
        public NonExistantColumnException(string columnName) : base(string.Format("Error attempting to access field: {0}.  This field does not exist in the current context.", columnName)) { }
        public NonExistantColumnException(string columnName, string type)
            : base(string.Format("Error attempting to access field: {0}.  This field does not exist in the type {1}.", columnName, type)) { }
    }
}
