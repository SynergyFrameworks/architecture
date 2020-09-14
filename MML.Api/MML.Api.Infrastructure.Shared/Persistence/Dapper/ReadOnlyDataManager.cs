using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dapper;
using log4net;
using MML.Enterprise.Common.Extensions;
using MML.Enterprise.Common.Mapping;
using MML.Enterprise.Persistence.Dapper.TypeHandlers;

namespace MML.Enterprise.Persistence.Dapper
{
    public class ReadOnlyDataManager : IReadOnlyDataManager
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof (ReadOnlyDataManager));
        private DapperRepository Repository { get; set; }
        public virtual string ConnectionString { get; set; }
        public virtual string[] QueryAssemblyProjects { get; set; }
        private Dictionary<Type, object> QueryHandlers { get; set; }
        private event EventHandler<Criteria> CriteriaInterceptor;
        public ICriteriaInterceptor CInterceptor { get; set; }
        private event EventHandler<QueryInfo> QueryInfoInterceptor;
        public IQueryInfoInterceptor QInterceptor { get; set; }
        public enum QueryTypes { FIND, TOTAL, FILTER, COUNT }

        public ReadOnlyDataManager() { }

        public void Initialize()
        {
            QueryHandlers = new Dictionary<Type, object>();

            SqlMapper.AddTypeHandler(new DateTimeHandler());

            if(QueryAssemblyProjects == null || !QueryAssemblyProjects.Any()){
                return;
            }

            foreach (var assemblyProject in QueryAssemblyProjects)
            {
                System.Reflection.Assembly project;

                try
                {
                    project = System.Reflection.Assembly.Load(assemblyProject);
                }
                catch (Exception)
                {
                    project = System.Reflection.Assembly.LoadFrom(assemblyProject);
                }

                var interfaceType = typeof (IQuery<>);
                
                var objects =
                    project.GetTypes()
                        .Where(
                            t =>
                                t.GetInterfaces()
                                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType) && !t.IsAbstract);

                foreach (var type in objects)
                {
                    var key = type.GetInterface("IQuery`1").GenericTypeArguments.FirstOrDefault();

                    if (key != null)
                    {
                        var val = Activator.CreateInstance(type);

                        QueryHandlers.Add(key,val);
                    }
                }
            }
        }

        private DapperRepository CurrentRepository
        {
            get { return Repository ?? (Repository = new DapperRepository(ConnectionString)); }
        }

        public T Find<T>(Criteria criteria)
        {
            return FindAll<T>(criteria).FirstOrDefault();
        }

        /**<summary>
         * This method assumes the name of the field you are filtering on is Id.
         * </summary> 
         */
        public T Find<T>(Guid id, Criteria criteria = null)
        {
            criteria = criteria ?? new Criteria();
            criteria.Parameters.Add("Id",id);
            return FindAll<T>(criteria).FirstOrDefault();
        }

        public IQuery<T> GetHandler<T>(Criteria criteria = null)
        {
            criteria = criteria ?? new Criteria();
            RunInterceptor<T>(criteria);
            if (QueryHandlers.ContainsKey(typeof(T)))
            {
                return QueryHandlers[typeof(T)] as IQuery<T>;
            }
            return null;
        }

        public QueryInfo GetQueryInfo<T>(IQuery<T> handler, QueryTypes queryType, Criteria criteria = null)
        {
            switch (queryType)
            {
                case QueryTypes.FIND:
                    return handler.GetQuery(criteria);
                case QueryTypes.FILTER:
                    return handler.GetFilterTypeResultSetQuery(criteria);
                case QueryTypes.COUNT:
                    return handler.GetCountQuery(criteria);
                case QueryTypes.TOTAL:
                    return handler.GetTotalsQuery(criteria);
                default:
                    return handler.GetQuery(criteria);
            }
        }


        public int GetCount<T>(Criteria criteria = null)
        {
            Log.Debug($"Running Count Query for {typeof(T)}");
            var handler = GetHandler<T>(criteria);
            if (handler != null)
            {
                var info = handler.GetCountQuery(criteria);
                if (info == null)
                    return 0;
                RunInterceptor<T>(info);
                return CurrentRepository.Query<RowCount>(info).FirstOrDefault().BlankIfNull().Count;
            }
            return 0;
        }

        public IList<T> FindAll<T>(Criteria criteria = null)
        {
            Log.Debug($"Running Find All Query for {typeof(T)}");
            var handler = GetHandler<T>(criteria);
            if (handler != null)
            {
                var info = handler.GetQuery(criteria);
                RunInterceptor<T>(info);
                return CurrentRepository.Query<T>(info).ToList();
            }
            return new List<T>();
        }

        /// <summary>
        /// Returns a DynamicResultSet with all the properties from the query in Data as well as:
        /// -SetType = typeof(T).Name
        /// -Metadata (currently empty)
        /// </summary>
        /// <typeparam name="T">Used as lookup key to find Query</typeparam>
        /// <param name="criteria"></param>
        /// <returns>a DynamicResultSet</returns>
        public DynamicResultSet DynamicFind<T>(Criteria criteria)
        {
            Log.Debug($"Running Dynamic Find Query for {typeof(T)}");
            var item = DynamicLookup<T>(QueryTypes.FIND, criteria).FirstOrDefault();
            return new DynamicResultSet
            {
                SetType = typeof(T).Name,
                Data = new List<IDictionary<string, object>>{(IDictionary<string, object>)item} 
            };
        }

        public IDictionary<string, object> DynamicTotal<T>(Criteria criteria)
        {
            var item = DynamicLookup<T>(QueryTypes.TOTAL, criteria).FirstOrDefault();
            return (IDictionary<string, object>)item;
        }

        /// <summary>
        /// Returns a DynamicResultSet with all the properties from the query in Data as well as:
        /// -SetType = typeof(T).Name
        /// -Metadata (currently empty)
        /// </summary>
        /// <typeparam name="T">Used as lookup key to find Query</typeparam>
        /// <param name="criteria"></param>
        /// <returns>a DynamicResultSet</returns>
        public DynamicResultSet DynamicFindAll<T>(Criteria criteria = null)
        {
            Log.Debug($"Running Dynamic Find All Query for {typeof(T)}");
            var list = DynamicLookup<T>(QueryTypes.FIND, criteria);
            var set = new DynamicResultSet
            {
                SetType = typeof(T).Name,
                Data = list.Select(d => (IDictionary<string,object>)d).ToList()
            };

            set.Metadata = GenerateMetadata(criteria, GetRowWithValues(set.Data));

            return set;
        }

        private IEnumerable<dynamic> DynamicLookup<T>(QueryTypes queryType, Criteria criteria = null)
        {
            var handler = GetHandler<T>(criteria);
            if (handler != null)
            {
                var info = GetQueryInfo<T>(handler, queryType, criteria);
                if(info == null)
                    return new List<dynamic>();
                RunInterceptor<T>(info);
                var results = CurrentRepository.DynamicQuery<T>(info).ToList();
                return results;
            }
            return new List<dynamic>();
        }

        private IDictionary<string, object> GetRowWithValues(IList<IDictionary<string, object>> rows)
        {
            if (!rows.Any() || !rows.First().Any())
                return new Dictionary<string, object>();
            var rowWithVals = new Dictionary<string, object>(rows.First());
            var missingVals = rowWithVals.Where(p => p.Value == null).ToDictionary(i => i.Key, i => false);
            if (!missingVals.Any())
                return rowWithVals;
            foreach (var row in rows)
            {
                var keys = missingVals.Select(i => i.Key).ToList();
                foreach (var val in keys)
                {
                    if (row[val] != null)
                    {
                        missingVals.Remove(val);
                        rowWithVals[val] = row[val];
                    }
                }

            }
            return rowWithVals;
        }

        private List<HeaderMetadata> GenerateMetadata(Criteria criteria, IDictionary<string, object> firstRow)
        {
            var metadata = new List<HeaderMetadata>();

            if (criteria == null || firstRow == null || !firstRow.Any())
                return metadata;

            var rangeKeys = criteria.DynamicHeaders ?? new List<string>();
            foreach (var prop in firstRow)
            {
                metadata.Add(new HeaderMetadata
                {
                    DisplayValue = prop.Key.Humanize(),
                    DataKey = prop.Key,
                    IsVisible = !(prop.Value is Guid),
                    IsRange = rangeKeys.Contains(prop.Key),
                    Type = prop.Value is DateTime ? "DateTime"
                        : prop.Value is int || prop.Value is int? ? "int"
                        : prop.Value is decimal || prop.Value is decimal? ? "decimal"
                        : prop.Value is Guid || prop.Value is Guid? ? "Guid"
                        : "string"
                });
            }
            return metadata;
        }

        private bool RunInterceptor<T>(Criteria criteria)
        {
            try
            {
                if (CInterceptor == null || criteria == null)
                    return false;
                if (CriteriaInterceptor == null)
                    CriteriaInterceptor += CInterceptor.OnQuery<T>;
                EventHandler<Criteria> handler = CriteriaInterceptor;
                if (handler != null)
                {
                    handler(null, criteria);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error Intercepting Criteria {0}", ex);
                throw;
            }
        }

        private bool RunInterceptor<T>(QueryInfo queryInfo)
        {
            try
            {
                if (QInterceptor == null || queryInfo == null)
                    return false;
                if (QueryInfoInterceptor == null)
                    QueryInfoInterceptor += QInterceptor.OnQuery<T>;
                EventHandler<QueryInfo> handler = QueryInfoInterceptor;
                if (handler != null)
                {
                    handler(null, queryInfo);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error Intercepting QueryInfo {0}", ex);
                throw;
            }
        }

        public T GetTotals<T>(Criteria criteria = null)
        {
            Log.Debug($"Running Totals Query for {typeof(T)}");
            var handler = GetHandler<T>(criteria);
            if (handler != null)
            {
                var info = handler.GetTotalsQuery(criteria);
                if(info == null)
                    return new List<T>().FirstOrDefault();
                RunInterceptor<T>(info);
                return CurrentRepository.Query<T>(info).FirstOrDefault();
            }
            return new List<T>().FirstOrDefault();
        }

        public IList<T> GetFilterTypeResultSet<T>(Criteria criteria = null)
        {
            Log.Debug($"Running Filter Query for {typeof(T)}");
            var handler = GetHandler<T>(criteria);
            if (handler != null)
            {
                var strippedCriteria = handler.CopyParametersWithoutColumnFilters(criteria);

                var info = handler.GetFilterTypeResultSetQuery(strippedCriteria);
                if (info == null)
                    return new List<T>();
                Log.Debug(info.Query);
                RunInterceptor<T>(info);
                return CurrentRepository.Query<T>(info);
            }
            return new List<T>();
        }

        public Dictionary<string, List<KeyValuePair<string, string>>> GetFilterTypes<T>(IList<T> results, Criteria criteria)
        {
            if (QueryHandlers.ContainsKey(typeof(T)))
            {
                var handler = QueryHandlers[typeof(T)] as IQuery<T>;
                if (handler != null)
                {
                    var types = handler.GetFilterTypes(results, criteria);
                    return types;
                }
            }
            return new Dictionary<string, List<KeyValuePair<string, string>>>();
        }

        public IList<IDynamicColumnValue> GetDynamicColumnHeaders<T>(Criteria criteria = null)
        {
            var handler = GetHandler<T>(criteria);
            if (handler != null)
            {
                var info = handler.GetDynamicColumnsListQuery(criteria);
                if (info == null)
                    return new List<DynamicColumnValue>().Cast<IDynamicColumnValue>().ToList();
                Log.Debug(info.Query);
                RunInterceptor<T>(info);
                return CurrentRepository.Query<DynamicColumnValue>(info).Cast<IDynamicColumnValue>().ToList();
            }
            return new List<DynamicColumnValue>().Cast<IDynamicColumnValue>().ToList();
        }
    }
}
