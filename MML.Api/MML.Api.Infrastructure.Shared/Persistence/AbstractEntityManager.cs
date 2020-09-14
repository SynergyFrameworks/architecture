using System;
using System.Collections.Generic;
using MML.Enterprise.Common.Persistence;
using MML.Enterprise.Persistence.Azure;

namespace MML.Enterprise.Persistence
{
    public abstract class AbstractEntityManager : IEntityManager
    {
        public abstract void Create(object obj);

        public abstract void Update(object obj);
        public virtual void Create<T>(IList<T> objs)
        {
            throw new NotImplementedException();
        }

        public virtual void Update<T>(IList<T> objs)
        {
            throw new NotImplementedException();
        }

        public virtual void Delete<T>(IList<T> objs)
        {
            throw new NotImplementedException();
        }

        public virtual void BulkCreateOrUpdate<T>(IList<T> objs)
        {
            throw new NotImplementedException();
        }

        public virtual T CreateOrUpdate<T>(T obj) where T : class, IPersistent 
        {
            throw new NotImplementedException();
        }

        public abstract void Delete(object obj);

        public virtual T SaveOrUpdate<T>(T obj) where T : class 
        {
            throw new NotImplementedException();
        }

        public virtual T Merge<T>(T obj) where T : class 
        {
            throw new NotImplementedException();
        }

        public virtual void Flush()
        {
            throw new NotImplementedException();
        }

        public abstract IList<T> FindAll<T>(IList<TableQueryParameters> parameters = null, bool preserveDateTimeKind = false) where T : class;

        public virtual IList<T> FindAllByNamedQuery<T>(string queryName, Dictionary<string, object> parameters) where T : class 
        {
            throw new NotImplementedException();
        }

        public virtual T FindByNamedQuery<T>(string queryName, Dictionary<string, object> parameters) where T : class 
        {
            throw new NotImplementedException();
        }

        public abstract T Find<T>(object id);
        public virtual IList<T> FindAll<T>(int startPage, int pageSize) where T : class 
        {
            throw new NotImplementedException();
        }

        public virtual IList<T> FindAllByNamedQuery<T>(string queryName, Dictionary<string, object> parameters, int startPage=0, int pageSize=0) where T : class 
        {
            throw new NotImplementedException();
        }

        public virtual IList<T> FindAll<T>(string partitionId, IList<TableQueryParameters> parameters = null, IList<string> columns = null, bool preserveDateTimeKind = false) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual IList<T> FindAllByNamedQuery<T>(string queryName, Dictionary<string, object> parameters, Dictionary<string, SortOrder> sorting, int startPage = 0, int pageSize = 0) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual void BatchCreate<T>(IList<T> objects, TimeSpan? preProcessingTime = null) where T : PersistentEntity
        {
            throw new NotImplementedException();
        }
        public virtual void BatchUpdate<T>(IList<T> objects, TimeSpan? preProcessingTime = null) where T : PersistentEntity
        {
            throw new NotImplementedException();
        }
        public virtual void BatchCreateOrUpdate<T>(IList<T> objects, TimeSpan? preProcessingTime = null) where T : PersistentEntity
        {
            throw new NotImplementedException();
        }
        public virtual void BatchDelete<T>(IList<T> objects, TimeSpan? preProcessingTime = null) where T : PersistentEntity
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<T> TakeFive<T>() where T : class, IPersistent
        {
            throw new NotImplementedException();
        }

        public virtual T Find<T>(object id, bool preserveDateTimeKind)
        {
            throw new NotImplementedException();
        }
        
        public virtual void EvictFromCache<T>(T evictee) where T : class, IPersistent
        {
            throw new NotImplementedException();
        }

        public virtual void ExecuteSQL(string sql, Dictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }
    }
}
