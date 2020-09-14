using Dapper;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

//http://stackoverflow.com/questions/23023534/managing-connection-with-non-buffered-queries-in-dapper
namespace MML.Enterprise.Persistence.Dapper
{
    public class DapperRepository : Repository
    {
        public static readonly ILog log = LogManager.GetLogger(typeof(DapperRepository));
        public DapperRepository(string connectionString) : base(connectionString)
        {
        }

        private object ChangeType(object value, Type type)
        {
            if (value == null && type.IsGenericType) return Activator.CreateInstance(type);
            if (value == null) return null;
            if (type == value.GetType()) return value;
            if (type.IsEnum)
            {
                if (value is string)
                    return Enum.Parse(type, value as string);
                else
                    return Enum.ToObject(type, value);
            }
            if (!type.IsInterface && type.IsGenericType)
            {
                Type innerType = type.GetGenericArguments()[0];
                object innerValue = ChangeType(value, innerType);
                return Activator.CreateInstance(type, new object[] { innerValue });
            }
            if (value is string && type == typeof(Guid)) return new Guid(value as string);
            if (value is string && type == typeof(Version)) return new Version(value as string);
            if (!(value is IConvertible)) return value;
            return Convert.ChangeType(value, type);
        }

        private dynamic CloneSub(dynamic parent, Type childType, string subName = "")
        {
            if (subName != "") subName = subName + "_";
            Object child = Activator.CreateInstance(childType);
            var childProperties = child.GetType().GetProperties();


            foreach (var childProperty in childProperties)
            {
                if (parent[subName + childProperty.Name] != null)
                {

                    if (childProperty.PropertyType.IsClass && childProperty.PropertyType.Name != "String")
                    {

                        childProperty.SetValue(child, CloneSub(parent, childProperty.PropertyType, childProperty.PropertyType.Name));
                    }
                    else
                    {
                        var dataChild = ChangeType(parent[subName + childProperty.Name].Value, childProperty.PropertyType);
                        childProperty.SetValue(child, dataChild);
                    }
                }
            }
            return child;
        }

        private T Clone<T>(dynamic parent)
        {
            T child = Activator.CreateInstance<T>();
            var childProperties = child.GetType().GetProperties();
            foreach (var childProperty in childProperties)
            {
                if (parent[childProperty.Name] != null)
                {
                    if (childProperty.PropertyType.IsClass && childProperty.PropertyType.Name != "String")
                    {

                        childProperty.SetValue(child, CloneSub(parent, childProperty.PropertyType, childProperty.PropertyType.Name));
                    }
                    else
                    {
                        var dataChild = ChangeType(parent[childProperty.Name].Value, childProperty.PropertyType);
                        childProperty.SetValue(child, dataChild);
                    }
                }
            }
            return child;
        }

        public T FindSP<T>(QueryInfo query)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                dynamic data = GetConnection(c => c.Query<dynamic>(query.Query, commandType: CommandType.StoredProcedure, param: query.Parameters, commandTimeout: query.CommandTimeout)).FirstOrDefault();

                if (data == null)
                    return default(T);
                
                var jsondata = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                var jsondedata = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(jsondata);
                return Clone<T>(jsondedata);
            }
            catch (Exception)
            {
                query.LogError(log);
                throw;
            }
            finally
            {
                log.Debug($"Executed query for {typeof(T).Name} in {sw.Elapsed}");
            }
        }
        public List<T> FindAllSP<T>(QueryInfo query)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                IEnumerable<dynamic> data = GetConnection(c => c.Query<dynamic>(query.Query, param: query.Parameters, commandType: CommandType.StoredProcedure, commandTimeout: query.CommandTimeout));
               
                if (data == null)
                    return null;
                List<T> result = new List<T>();
                foreach (var dynData in data)
                {
                    var jsondata = Newtonsoft.Json.JsonConvert.SerializeObject(dynData);
                    var jsondedata = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(jsondata);
                    result.Add(Clone<T>(jsondedata));
                }

                return result;
            }
            catch (Exception)
            {
                query.LogError(log);
                throw;
            }
            finally
            {
                log.Debug($"Executed query for {typeof(T).Name} in {sw.Elapsed}");
            }
        }
      
        public T Find<T>(QueryInfo query)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                dynamic data = GetConnection(c => c.Query<dynamic>(query.Query, param: query.Parameters, commandTimeout: query.CommandTimeout)).FirstOrDefault();

                if (data == null)
                    return default(T);
        
                var jsondata = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                var jsondedata = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(jsondata);
                return Clone<T>(jsondedata);
            
            }
            catch (Exception)
            {
                query.LogError(log);
                throw;
            }
            finally
            {
                log.Debug($"Executed query for {typeof(T).Name} in {sw.Elapsed}");
            }
        }

        public List<T> FindAll<T>(QueryInfo query)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                IEnumerable<dynamic> data = GetConnection(c => c.Query<dynamic>(query.Query, param: query.Parameters, commandTimeout: query.CommandTimeout));

                List<T> result = new List<T>();
                foreach (var dynData in data)
                {
                    var jsondata = Newtonsoft.Json.JsonConvert.SerializeObject(dynData);
                    var jsondedata = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(jsondata);
                    result.Add(Clone<T>(jsondedata));
                }

                return result.ToList<T>();
            }
            catch (Exception)
            {
                query.LogError(log);
                throw;
            }
            finally
            {
                log.Debug($"Executed query for {typeof(T).Name} in {sw.Elapsed}");
            }
        }

        public void Execute(QueryInfo query)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = GetConnection(c => c.ExecuteAsync(sql: query.Query, param: query.Parameters));
            }
            catch (Exception)
            {
                query.LogError(log);
                throw;
            }
            finally
            {
                log.Debug($"Executed query for update in {sw.Elapsed}");
            }
        }

        public IList<T> Query<T>(QueryInfo query)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return GetConnection(c => c.Query<T>(query.Query, query.Parameters, commandTimeout: query.CommandTimeout)).ToList();
            }
            catch (Exception)
            {
                query.LogError(log);
                throw;
            }
            finally
            {
                log.Debug($"Executed query for {typeof(T).Name} in {sw.Elapsed}");
            }
        }

        public IList<dynamic> DynamicQuery<T>(QueryInfo query)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return GetConnection(c => c.Query<dynamic>(query.Query, query.Parameters, commandTimeout: query.CommandTimeout)).ToList();
            }
            catch (Exception)
            {
                query.LogError(log);
                throw;
            }
            finally
            {
                log.Debug($"Executed dynamic query for {typeof(T).Name} in {sw.Elapsed}");
            }
        }

        public IList<T> QueryUnbuffered<T, M>(Func<IEnumerable<M>, IEnumerable<T>> process, QueryInfo query)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return GetConnection(c => c.Query<M>(query.Query, query.Parameters, buffered: false, commandTimeout: query.CommandTimeout), process).ToList();
            }
            catch (Exception)
            {
                query.LogError(log);
                throw;
            }
            finally
            {
                log.Debug($"Executed unbuffered query for {typeof(T).Name} in {sw.Elapsed}");
            }
        }

        public void Create(QueryInfo query)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = GetConnection(c => c.Query(sql: query.Query, param: query.Parameters));
            }
            catch (Exception)
            {
                query.LogError(log);
                throw;
            }
            finally
            {
                log.Debug($"Executed query for insert in {sw.Elapsed}");
            }
        }

        public void Update(QueryInfo query)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = GetConnection(c => c.Query(sql: query.Query, param: query.Parameters));
            }
            catch (Exception)
            {
                query.LogError(log);
                throw;
            }
            finally
            {
                log.Debug($"Executed query for update in {sw.Elapsed}");
            }
        }
        public void Delete(QueryInfo query)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = GetConnection(c => c.Query(sql: query.Query, param: query.Parameters));
            }
            catch (Exception)
            {
                query.LogError(log);
                throw;
            }
            finally
            {
                log.Debug($"Executed query for update in {sw.Elapsed}");
            }
        }


    }
}
