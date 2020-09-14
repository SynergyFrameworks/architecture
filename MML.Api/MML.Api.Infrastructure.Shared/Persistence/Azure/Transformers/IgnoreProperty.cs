using System;

namespace MML.Enterprise.Persistence.Azure.Transformers
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreProperty : Attribute
    {
    }
}
