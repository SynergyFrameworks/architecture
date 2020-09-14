using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Persistence.Azure
{
    public interface IAzureEntityManager
    {
        void Create(PersistentEntity obj);
        void Update(PersistentEntity obj);
        void Delete(string container, string clientId, string objectId);
        void Delete<T>(string clientId, string objectId);
        IList<T> FindAll<T>() where T : PersistentEntity;
        T Find<T>(string clientId, string rowId);
        IList<T> FindAllByClient<T>(string clientId) where T : PersistentEntity;
        /// <summary>
        /// Retreives a collection 
        /// </summary>
        /// <typeparam name="TP">The parent type</typeparam>
        /// <typeparam name="TC">The collection type</typeparam>
        /// <param name="clientId">the Client id</param>
        /// <param name="parentId">the parent object id</param>
        /// <returns></returns>
        IList<TC> FindAllCollectionByType<TP, TC>(string clientId, string parentId);
        IList<T> FindAllCollectionByContainerName<T>(string containerName, string clientId, string parentId);
        void AddToCollection<T>(PersistentEntity obj);
        void AddToCollection(string container, PersistentEntity obj);        
    }
}
