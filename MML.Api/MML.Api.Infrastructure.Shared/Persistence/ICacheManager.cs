using System.Collections.Generic;

namespace MML.Enterprise.Persistence
{
    public interface ICacheManager
    {
        void Save<T>(List<T> data,Criteria criteria);
        List<T> Get<T>(string type,Criteria criteria);
        List<T> Get<T>(string key); 
        void RemoveCache(string type, Criteria criteria);
        void ClearCache();
    }
}
