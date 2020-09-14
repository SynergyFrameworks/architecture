using System;
using MML.Enterprise.Common.Persistence;
using MML.Enterprise.Persistence.Azure.Transformers;

namespace MML.Enterprise.Persistence.Azure
{
    [Serializable]
    public class PersistentEntity : IPersistent
    {
        public virtual string PartitionKey { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime LastModifiedDate { get; set; }
        public virtual string CreatedBy { get; set; }
        public virtual string LastModifiedBy { get; set; }
        public virtual bool IsInactive { get; set; }
        [IgnoreProperty]
        public virtual Guid Id { get; set; }
        public virtual string RowKey { get; set; }
    }
}
