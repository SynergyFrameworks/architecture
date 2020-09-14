using System;

namespace MML.Enterprise.Common.Persistence
{
    public class PersistenceException : Exception
    {
        public PersistenceException(String msg) : base(msg)
        {
        }

        public PersistenceException(String msg, Exception e) : base(msg, e)
        {
        }
    }
}
