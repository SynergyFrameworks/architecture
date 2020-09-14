//using System;
//using System.Collections.Generic;
//using System.Runtime.Caching;
//using Microsoft.Extensions.Caching.Memory;
//using Newtonsoft.Json;


//namespace MML.Enterprise.Persistence.Cache
//{
//    public class CacheManager:ICacheManager
//    {
//        private static readonly MemoryCache Cache = Microsoft.Extensions.Caching.Memory.MemoryCache.Default;
//        public List<T> Get<T>(string type, Criteria criteria)
//        {
//            var key = GenerateKey(type, criteria);
//            return (List<T>) Cache.Get(key);
//        }

//        public List<T> Get<T>(string key)
//        {
//            return (List<T>) Cache.Get(key);
//        } 

//        public void Save<T>(List<T> data, Criteria criteria)
//        {
//            var key = GenerateKey(typeof(T).Name, criteria);
//            Cache.Add(key, data, new CacheItemPolicy());
//        }

//        public void RemoveCache(string type, Criteria criteria)
//        {
//            var key = GenerateKey(type, criteria);
//            Cache.Remove(key);
//        }
//        public void ClearCache()
//        {
//            Cache.Dispose();
//        }

//        private string GenerateKey(string type, Criteria criteria)
//        {
//            return type + JsonConvert.SerializeObject(criteria);
//        }
//    }
//}
