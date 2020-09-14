using Dapper;
using log4net;
using MML.Enterprise.Common.Extensions;
using MML.Enterprise.Common.Persistence;
using MML.Enterprise.Persistence.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;

namespace MML.Enterprise.Persistence.Dapper
{
    public class DapperEntityManager : IDapperEntityManager
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DapperEntityManager));
        public IDictionary<string, string> TableMapping { get; set; }
        public string tableType { get; set; }
        private DapperRepository Repository { get; set; }
        public virtual string ConnectionString { get; set; }
        private bool IsMultiTenant { get; set; }
        private string TenantClaim { get; set; }
        private string JoinString { get; set; } = "";
        private DapperRepository CurrentRepository => Repository ?? (Repository = new DapperRepository(ConnectionString));
        protected Guid CurrentIdentity
        {
            get
            {
                var data = Guid.Empty;
                if (IsMultiTenant)
                {

                    var tenant = Thread.GetData(Thread.GetNamedDataSlot("tenant"));
                    if (tenant != null)
                    {
                        if (tenant as Guid? != null)
                        {
                            data = (Guid)(tenant as Guid?);
                        }
                        else
                        {
                            var tenantId = tenant.GetPropertyValue("Id");
                            if (tenantId != null && tenantId as Guid? != null)
                            {
                                data = (Guid)(tenantId as Guid?);
                            }
                        }
                    }
                    else if (Thread.CurrentPrincipal == null)
                    {
                        data = Guid.Empty;
                    }
                    else
                    {
                        var identity = (ClaimsIdentity)Thread.CurrentPrincipal.Identity;
                        var tenantClaim = identity.FindFirst(TenantClaim);

                        data = tenantClaim == null ? Guid.Empty : Guid.Parse(tenantClaim.Value);

                    }
                    if (data != Guid.Empty)
                    {
                        var tenantId = data;
                        log.DebugFormat("Enable tenant filter and setting tenant_id to {0}", tenantId);
                    }
                }
                return data;
            }
        }
       
        public void Create(object obj)
        {
            QueryInfo info = new QueryInfo();

            CurrentRepository.Create(GenerateCreateQuery(obj));
        }
        private bool isAuditTable()
        {
            return (tableType == AuditType.LOG.ToString() || tableType == AuditType.FULL.ToString());
        }

        private bool isAuditLogTable()
        {
            return (tableType == AuditType.LOG.ToString());
        }

        private bool checkPropertyAndMapColumn(object obj, string key)
        {
            return (obj.GetType().GetProperty(key) != null);
        }

        private bool isAuditTypeNone()
        {
            return (tableType == AuditType.NONE.ToString());
        }

        private QueryInfo GenerateCreateQuery(object obj)
        {
            MapTableTypeRef(obj);
            var queryInfo = new QueryInfo();
            QueryColumnMapping(obj, out string modifiedByColumn, out string modifiedcreatedByParam, out string modifiedDateByColumn, out string modifiedDateByParam, out string createdByColumn, out string createdByParam, out string createdDateByColumn, out string createdDateByParam, out string tenantByColumn, out string tenantByParam);
            queryInfo.Query = string.Format(@"INSERT INTO {0} ({1}{9}{11}{7}{5}{3}) VALUES ({2}{10}{12}{8}{6}{4})",
                TableMapping["table"], string.Join(", ", //0
                TableMapping.Values.Where(x => x != TableMapping["table"])), //1 & 1
                "@" + string.Join(",@", TableMapping.Keys.Where(x => x != "table")), //2 & 1 
         
                modifiedByColumn, //5
                modifiedcreatedByParam, //6
                modifiedDateByColumn, //7
                modifiedDateByParam, //8
                createdByColumn, //9
                createdByParam, //10
                createdDateByColumn, //11
                createdDateByParam, //12
                tenantByColumn, //13
                tenantByParam); //14
            MapDynamicParameters(obj, queryInfo);
            return queryInfo;
        }

        private void MapDynamicParameters(object obj, QueryInfo queryInfo)
        {
            queryInfo.Parameters = new DynamicParameters();

            foreach (var keyvalue in TableMapping.Keys.Where(x => x != "table"))
            {
                if (keyvalue != "TenantId")
                {
                    if (keyvalue == "Id")
                        queryInfo.Parameters.Add(keyvalue, Guid.NewGuid());

                    if (keyvalue != "Id")
                        queryInfo.Parameters.Add(keyvalue, GetValueForObject(obj, keyvalue));

                }

            }

            MapCustomParameters(obj, queryInfo);
        }

        private void MapCustomParameters(object obj, QueryInfo queryInfo)
        {
            var identity = (ClaimsIdentity)Thread.CurrentPrincipal.Identity;

            if (isAuditTable())
                queryInfo.Parameters.Add("CreatedBy", Guid.Parse(identity.FindFirst(ClaimTypes.Role).Value));

            if (isAuditTable())
                queryInfo.Parameters.Add("CreatedDate", DateTime.UtcNow);

            if (isAuditTable() && !isAuditLogTable())
                queryInfo.Parameters.Add("LastModifiedBy", Guid.Parse(identity.FindFirst(ClaimTypes.Role).Value));

            if (isAuditTable() && !isAuditLogTable())
                queryInfo.Parameters.Add("LastModifiedDate", DateTime.UtcNow);

            if (checkPropertyAndMapColumn(obj, "IsInactive") && !isAuditLogTable())
                queryInfo.Parameters.Add("IsInactive", GetValueForObject(obj, "IsInactive"));

            queryInfo.Parameters.Add("TenantId", CurrentIdentity);
        }

        private void QueryColumnMapping(object obj,/* out string inactiveColumn, out string inactiveParam,*/ out string modifiedByColumn, out string modifiedcreatedByParam, out string modifiedDateByColumn, out string modifiedDateByParam, out string createdByColumn, out string createdByParam, out string createdDateByColumn, out string createdDateByParam, out string tenantByColumn, out string tenantByParam)
        {


   
            modifiedByColumn = ColumnBind("LAST_MODIFIED_BY", checkPropertyAndMapColumn(obj, "LastModifiedBy") && !isAuditLogTable() && !isAuditTypeNone());
            modifiedcreatedByParam = ColumnPrefixBind("LastModifiedBy", checkPropertyAndMapColumn(obj, "LastModifiedBy") && !isAuditLogTable() && !isAuditTypeNone());
            modifiedDateByColumn = ColumnBind("LAST_MODIFIED_DATE", checkPropertyAndMapColumn(obj, "LastModifiedDate") && !isAuditLogTable() && !isAuditTypeNone());
            modifiedDateByParam = ColumnPrefixBind("LastModifiedDate", checkPropertyAndMapColumn(obj, "LastModifiedDate") && !isAuditLogTable() && !isAuditTypeNone());


            createdByColumn = ColumnBind("CREATED_BY", isAuditTable());
            createdByParam = ColumnPrefixBind("CreatedBy", isAuditTable());
            createdDateByColumn = ColumnBind("CREATED_DATE", isAuditTable());
            createdDateByParam = ColumnPrefixBind("CreatedDate", isAuditTable());
            tenantByColumn = ", tenant_id";
            tenantByParam = ", @TenantId";
            if (isAuditTypeNone())
            {

                createdByColumn = "";
                createdByParam = "";

                createdDateByColumn = "";
                createdDateByParam = "";

                modifiedDateByColumn = "";
                modifiedDateByParam = "";

                modifiedByColumn = "";
                modifiedcreatedByParam = "";

            }
        }

        private string ColumnPrefixBind(string value, bool condition)
        {
            return ColumnBind(value, condition, true);
        }

        private string ColumnBind(string value, bool condition, bool isPreCursor = false)
        {
            return condition ? string.Format(", {1}{0}", value, isPreCursor ? "@" : "") : string.Empty;
        }
        private QueryInfo MapTableTypeRef(object obj, bool isSelect = false)
        {
            TableMapping = TableMapper.GetTableMapping(obj, isSelect: isSelect);
            tableType = TableMapping["tableType"];
            TableMapping.Remove("tableType");
            var queryInfo = new QueryInfo();
            return queryInfo;
        }
        private QueryInfo GenerateSelectQueryId(object obj)
        {
            JoinString = "";
            var queryInfo = MapTableTypeRef(obj, isSelect: true);
            queryInfo.Query = string.Format("SELECT {0} FROM {1} {3} WHERE {2}=@Id", MapSelectObject(TableMapping), TableMapping["table"], TableMapping["Id"], JoinString);
            queryInfo.Parameters = new DynamicParameters();
            queryInfo.Parameters.Add("Id", GetValueForObject(obj, "Id"));
            return queryInfo;
        }

        private string GenerateWhereStatement(IList<TableQueryParameters> parameters, string aliasName)
        {

            if (parameters != null && parameters.Any())
            {
                var counter = 0;
                var whereQuery = string.Empty;
                foreach (var k in parameters)
                {
                    counter++;
                    var andString = counter < parameters.Count ? " AND " : "";
                    whereQuery += string.Format("[{2}].{0}=@{0}{1}", k.PropertyName, andString, aliasName);
                }
                if (!string.IsNullOrEmpty(whereQuery))
                    whereQuery = " WHERE " + whereQuery;

                return whereQuery;

            }

            return string.Empty;

        }
        private QueryInfo GenerateSelectQuery(object obj, IList<TableQueryParameters> parameters)
        {
            JoinString = "";

            var standardTable = obj.GetType().Name.Contains("Standard");
            var param = standardTable ? null : new TableQueryParameters { PropertyName = "tenant_id", Value = CurrentIdentity };

            if (parameters == null)
                parameters = new List<TableQueryParameters>();

            if (!standardTable)
            {
                parameters.Add(param);
            }
            var queryInfo = MapTableTypeRef(obj, isSelect: true);

            //TableMapping.AddRange(GetDefaultMapping());
            queryInfo.Query = string.Format("SELECT {0} FROM {1} {2} {3}", MapSelectObject(TableMapping), TableMapping["table"], JoinString, GenerateWhereStatement(parameters, TableMapping["table"]));
            queryInfo.Parameters = new DynamicParameters();

            if (parameters != null && parameters.Any())
            {
                foreach (var k in parameters)
                {
                    queryInfo.Parameters.Add(k.PropertyName, k.Value);
                }
            }

            return queryInfo;
        }

        private QueryInfo GenerateDeleteQuery(object obj)
        {
            MapTableTypeRef(obj);
            var queryInfo = new QueryInfo();
            queryInfo.Query = string.Format("DELETE FROM {0} WHERE {1}= @Id", TableMapping["table"], TableMapping["Id"]);
            queryInfo.Parameters = new DynamicParameters();
            queryInfo.Parameters.Add("Id", GetValueForObject(obj, "Id"));
            return queryInfo;
        }

        private QueryInfo GenerateUpdateQuery(object obj)
        {
            MapTableTypeRef(obj);
            var queryInfo = new QueryInfo();
            MapUpdateColumnQuery(obj, /*out string inactiveColumn,*/ out string lastModifiedByColumn, out string lastModifiedDateColumn);
            queryInfo.Query = string.Format(@"UPDATE {0}	
            SET {1}{3}{4} WHERE {2} AND tenant_id = @TenantId ", TableMapping["table"], MapObject(TableMapping), MapObject(TableMapping, true)/*, inactiveColumn*/, lastModifiedByColumn, lastModifiedDateColumn);
            UpdateParameterMapping(obj, queryInfo);
            return queryInfo;
        }

        private void UpdateParameterMapping(object obj, QueryInfo queryInfo)
        {
            queryInfo.Parameters = new DynamicParameters();
            foreach (var key in TableMapping.Keys.Where(x => x != "table"))
            {
                queryInfo.Parameters.Add(key, GetValueForObject(obj, key));
            }

            var identity = (ClaimsIdentity)Thread.CurrentPrincipal.Identity;
            queryInfo.Parameters.Add("LastModifiedBy", Guid.Parse(identity.FindFirst(ClaimTypes.Role).Value));
            queryInfo.Parameters.Add("LastModifiedDate", DateTime.UtcNow);
            //if (checkPropertyAndMapColumn(obj, "IsInactive"))
            //{
            //    queryInfo.Parameters.Add("IsInactive", GetValueForObject(obj, "IsInactive"));
            //}

            queryInfo.Parameters.Add("TenantId", CurrentIdentity);
        }

        private void MapUpdateColumnQuery(object obj/*, out string inactiveColumn*/, out string lastModifiedByColumn, out string lastModifiedDateColumn)
        {
            //inactiveColumn = ColumnBind("INACTIVE_INDICATOR = @IsInactive ", checkPropertyAndMapColumn(obj, "IsInactive"));
            lastModifiedByColumn = ColumnBind("LAST_MODIFIED_BY = @LastModifiedBy", !isAuditTypeNone());
            lastModifiedDateColumn = ColumnBind("LAST_MODIFIED_DATE = @lastModifiedDate ", !isAuditTypeNone());
            //if (isAuditTypeNone())
            //{
            //    inactiveColumn = "";
            //}
        }

        private string MapSelectObject(IDictionary<string, string> tableMapping, bool isUnique = false)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (!isUnique)

            {
                foreach (var dictObj in tableMapping)
                {
                    if (dictObj.Key != "table")
                    {
                        if (!dictObj.Key.Contains("$"))
                            stringBuilder.AppendFormat("[{2}].[{1}] AS [{0}],", dictObj.Key, dictObj.Value, tableMapping["table"]);
                        else
                        {

                            var childTable = dictObj.Key.Split('$')[0];
                            var childAttrib = dictObj.Key.Split('$')[1];
                            if (childAttrib != "table" && childAttrib != "tableType")
                            {
                                stringBuilder.AppendFormat("[{2}].[{1}] AS [{0}],", childTable + "_" + childAttrib, dictObj.Value, childTable);
                            }
                            if (childAttrib == "table")
                                JoinString = string.Format(" Left Join [{0}] as [{3}] on [{3}].[{2}] = [{1}].[{2}] ", dictObj.Value, tableMapping["table"], tableMapping[childTable + "$Id"], childTable);
                        }
                    }
                }
            }
            else
            {
                var dictArr = tableMapping.Where(x => x.Key == "Id");
                if (dictArr.Any())
                {
                    var dictObj = dictArr.First();
                    stringBuilder.AppendFormat("[{1}] = @{0},", dictObj.Key, dictObj.Value);
                }
            }
            return stringBuilder.ToString().TrimEnd(',');
        }

        private string MapObject(IDictionary<string, string> tableMapping, bool isUnique = false)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (!isUnique)

            {
                foreach (var dictObj in tableMapping)
                {
                    if (dictObj.Key != "Id" && dictObj.Key != "table")
                    {
                        stringBuilder.AppendFormat("[{1}] = @{0}, ", dictObj.Key, dictObj.Value);
                    }
                }
            }
            else
            {
                var dictArr = tableMapping.Where(x => x.Key == "Id");
                if (dictArr.Any())
                {
                    var dictObj = dictArr.First();
                    stringBuilder.AppendFormat("[{1}] = @{0}, ", dictObj.Key, dictObj.Value);
                }
            }
            return stringBuilder.ToString().TrimEnd(',', ' ');
        }


        private dynamic GetValueForObject(object data, string keyvalue)
        {

            System.Reflection.PropertyInfo pi = data.GetType().GetProperty(keyvalue);

            if (pi.PropertyType.Name != "String" && pi.PropertyType.IsClass)
            {
                dynamic result = (pi.GetValue(data, null));
                if (result != null && result.GetType().GetProperty("Id") != null)
                {
                    var dataReq = result.Id;
                    return dataReq;
                }
                return result;

            }
            else
            {
                if (pi == null && IsTenant(keyvalue))
                {
                    return Guid.Empty;
                }

                dynamic result = (pi.GetValue(data, null));
                return result;
            }
        }

        private static bool IsTenant(string keyvalue)
        {
            return keyvalue == "TenantId";
        }

        public void SetMapping(IDictionary<string, string> obj)
        {
            TableMapping = obj;
        }


        //public void Create<T>(IList<T> objs)
        //{
        //    try
        //    {
        //        objs.ForEach(obj => Create(obj));
        //    }
        //    catch (Exception e)
        //    {
        //        log.Error(e.Message, e);
        //        throw new PersistenceException(e.Message, e);
        //    }
        //}


        public void Delete<T>(IList<T> objs)
        {
            throw new NotImplementedException();
        }

        public void BulkCreateOrUpdate<T>(IList<T> objs)
        {
            throw new NotImplementedException();
        }

       
        T IEntityManager.CreateOrUpdate<T>(T obj)
        {
            if (obj.Id == Guid.Empty)
            {
                log.InfoFormat("Calling create on type {0}", typeof(T).Name);
                Create(obj);
                return obj;
            }

            log.InfoFormat("Calling update on type {0}", typeof(T).Name);
            Update(obj);
            return obj;

        }

        void IEntityManager.EvictFromCache<T>(T evictee)
        {
            throw new NotImplementedException();
        }

        public void BatchCreate<T>(IList<T> objects, TimeSpan? preProcessingTime = null) where T : PersistentEntity
        {
            throw new NotImplementedException();
        }

        public void BatchUpdate<T>(IList<T> objects, TimeSpan? preProcessingTime = null) where T : PersistentEntity
        {
            throw new NotImplementedException();
        }

        public void BatchCreateOrUpdate<T>(IList<T> objects, TimeSpan? preProcessingTime = null) where T : PersistentEntity
        {
            throw new NotImplementedException();
        }

        public void BatchDelete<T>(IList<T> objects, TimeSpan? preProcessingTime = null) where T : PersistentEntity
        {
            throw new NotImplementedException();
        }

        public void Delete(object obj)
        {
            QueryInfo info = new QueryInfo();
            CurrentRepository.Delete(GenerateDeleteQuery(obj));
        }

        public void ExecuteSQL(string sql, Dictionary<string, object> parameters)
        {
            QueryInfo info = new QueryInfo();
            info.Query = sql;
            if (parameters == null && !parameters.Any()) return;

            foreach (var param in parameters)
            {

                info.Parameters.SetProperty(param.Key, param.Value);

            }

            CurrentRepository.Execute(info);
        }

        public T SaveOrUpdate<T>(T obj) where T : class
        {
            //if (obj.Id == Guid.Empty)
            //{
            //    log.InfoFormat("Calling create on type {0}", typeof(T).Name);
            //    Create(obj);
            //    return obj;
            //}

            //log.InfoFormat("Calling update on type {0}", typeof(T).Name);
            //Update(obj);
            return obj;
        }

        public T Merge<T>(T obj) where T : class
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        private QueryInfo ProcessParamSP(string spName, IList<TableQueryParameters> parameters)
        {
            var queryInfo = new QueryInfo();
            queryInfo.Query = spName.Trim();
            queryInfo.Parameters = new DynamicParameters();
            foreach (var _parameters in parameters)
            {
                queryInfo.Parameters.Add(_parameters.PropertyName, _parameters.Value);
            }
            return queryInfo;
        }


        public T FindSp<T>(string spName, IList<TableQueryParameters> parameters = null)
        {
            var result = CurrentRepository.FindSP<T>(ProcessParamSP(spName, parameters));
            return result;
        }

        public IList<T> FindAllSp<T>(string spName, IList<TableQueryParameters> parameters = null)
        {
            return CurrentRepository.FindAllSP<T>(ProcessParamSP(spName, parameters));

        }

        public IList<T> FindAll<T>(IList<TableQueryParameters> parameters = null, bool preserveDateTimeKind = false) where T : class
        {
            dynamic retObj = Activator.CreateInstance<T>();
            return CurrentRepository.FindAll<T>(GenerateSelectQuery(retObj, parameters));
        }


        public IList<T> FindAll<T>(IList<TableQueryParameters> parameters) where T : class
        {
            dynamic retObj = Activator.CreateInstance<T>();
            return CurrentRepository.FindAll<T>(GenerateSelectQuery(retObj, parameters));

        }


        /// <summary>
        /// Explicit Casting And ToList was required for ReferenceController to resolve var
        /// findMethod = ReferenceDataManager.GetType().GetMethod("FindAll").MakeGenericMethod(type);
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <param name="startPage"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public IList<T> FindAll<T>(IList<TableQueryParameters> parameters = null, int startPage = 0, int pageSize = 0) where T : class
        {
            


            dynamic retObj = Activator.CreateInstance<T>();
            List<T> query = CurrentRepository.FindAll<T>(GenerateSelectQuery(retObj, parameters));

            if (query.Count != 0 && pageSize != 0 && startPage != 0)
                return query.Skip((startPage - 1) * pageSize).Take(pageSize).ToList();

            return query;
        }

        public T FindByNamedQuery<T>(string queryName, Dictionary<string, object> parameters) where T : class
        {
            throw new NotImplementedException();
        }

        T IEntityManager.Find<T>(object id)
        {
            dynamic retObj = Activator.CreateInstance<T>();
            retObj.Id = Guid.Parse(id.ToString());
            var result = CurrentRepository.Find<T>(GenerateSelectQueryId(retObj));
            return result;

        }

        public T Find<T>(object id)
        {
            var tmp = (T)CurrentRepository.Query<T>(GenerateSelectQueryId(id));
            return tmp;

        }

        public IList<T> FindAll<T>(int startPage, int pageSize) where T : class
        {
            dynamic retObj = Activator.CreateInstance<T>();
            var query = CurrentRepository.FindAll<T>(GenerateSelectQuery(retObj, null));
            return query.Skip((startPage - 1) * pageSize).Take(pageSize).ToList();

        }

        public T Find<T>(object id, bool preserveDateTimeKind)
        {
            throw new NotImplementedException();
        }

        public IList<T> FindAllByNamedQuery<T>(string queryName, Dictionary<string, object> parameters, int startPage = 0, int pageSize = 0) where T : class
        {
            throw new NotImplementedException();
        }

        public IList<T> FindAll<T>(string partitionId, IList<TableQueryParameters> parameters = null, IList<string> columns = null, bool preserveDateTimeKind = false) where T : class
        {
            throw new NotImplementedException();
        }

        public IList<T> FindAllByNamedQuery<T>(string queryName, Dictionary<string, object> parameters, Dictionary<string, SortOrder> sorting, int startPage = 0, int pageSize = 0) where T : class
        {
            throw new NotImplementedException();
        }

        public void Update<T>(IList<T> objs)
        {
            try
            {
             //   objs.ForEach(obj => Update(obj));
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw new PersistenceException(e.Message, e);
            }
        }

        public void Update(object obj)
        {
            QueryInfo info = new QueryInfo();

            CurrentRepository.Update(GenerateUpdateQuery(obj));
        }

        public IList<T> FindAllByNamedQuery<T>(string queryName, Dictionary<string, object> parameters) where T : class
        {
            throw new NotImplementedException();
        }

        public void Create<T>(IList<T> objs)
        {
            throw new NotImplementedException();
        }

        public enum AuditType
        {
            FULL, LOG, NONE
        }
    }
}
