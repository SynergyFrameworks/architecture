using System;

namespace MML.Enterprise.Persistence
{
    public class SqlPagingException : Exception
    {
        public SqlPagingException() : base() { }
        public SqlPagingException(string message) : base(message)
        {
        }
    }
}
