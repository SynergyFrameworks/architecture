using System;
using System.Collections.Generic;
using MML.Enterprise.Common.Persistence;
using MML.Enterprise.Persistence.Azure;

namespace MML.Enterprise.Persistence
{
    public interface IEntityManager
    {
        void Create(object obj);
        void Update(object obj);
        void Create<T>(IList<T> objs);
        void Update<T>(IList<T> objs);
        void Delete<T>(IList<T> objs);
        void BulkCreateOrUpdate<T>(IList<T> objs);
        T CreateOrUpdate<T>(T obj) where T : class, IPersistent;

        void EvictFromCache<T>(T evictee) where T : class, IPersistent;
        void BatchCreate<T>(IList<T> objects, TimeSpan? preProcessingTime = null) where T : PersistentEntity;
        void BatchUpdate<T>(IList<T> objects, TimeSpan? preProcessingTime = null) where T : PersistentEntity;
        void BatchCreateOrUpdate<T>(IList<T> objects, TimeSpan? preProcessingTime = null) where T : PersistentEntity;
        void BatchDelete<T>(IList<T> objects, TimeSpan? preProcessingTime = null) where T : PersistentEntity;       
        void Delete(object obj);
        void ExecuteSQL(string sql, Dictionary<string,object> parameters);
        T SaveOrUpdate<T>(T obj) where T : class;
        T Merge<T>(T obj) where T : class;
        void Flush();
        IList<T> FindAll<T>(IList<TableQueryParameters> parameters = null, bool preserveDateTimeKind = false) where T : class;
        IList<T> FindAllByNamedQuery<T>(string queryName, Dictionary<string, object> parameters) where T: class;
        T FindByNamedQuery<T>(string queryName, Dictionary<string,object> parameters) where T: class;
        T Find<T>(object id);
        T Find<T>(object id, bool preserveDateTimeKind);
        IList<T> FindAll<T>(int startPage, int pageSize) where T : class;
        IList<T> FindAllByNamedQuery<T>(string queryName, Dictionary<string, object> parameters, int startPage =0, int pageSize =0) where T : class;
        IList<T> FindAll<T>(string partitionId, IList<TableQueryParameters> parameters = null, IList<string> columns = null, bool preserveDateTimeKind = false) where T : class;

        IList<T> FindAllByNamedQuery<T>(string queryName, Dictionary<string, object> parameters, Dictionary<string, SortOrder> sorting, int startPage = 0, int pageSize = 0) where T : class;
    }
}
