//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Linq.Dynamic;
//using System.Net;
//using System.Reflection;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using System.Web.Hosting;
//using log4net;
//using Microsoft.WindowsAzure.Storage;
//using Microsoft.WindowsAzure.Storage.Table;
//using Microsoft.WindowsAzure.Storage.Table.Queryable;
//using MML.Enterprise.Common.Persistence;
//using MML.Enterprise.Persistence.Azure.Extensions;
//using MML.Enterprise.Persistence.Azure.Transformers;

//namespace MML.Enterprise.Persistence.Azure
//{
//    public class AzureTableManager : AbstractEntityManager
//    {
//        private static readonly ILog Log = LogManager.GetLogger(typeof (AzureTableManager));
//        private DteTransformer BaseTransformer { get; set; }
//        private event EventHandler<PersistentEntity> UpdateInterceptor;
//        public IAzureInterceptor Interceptor { get; set; }
//        public string ConnectionString { get; set; }
//        private CloudStorageAccount _storageAccount;
//        public bool PrependTableNames { get; set; }
//        public int CompressJson { get; set; }
//        private string _tablePrependString = "";
//        private const int Batchsize = 100;
//        private enum BatchOperations
//        {
//            Create,
//            Update,
//            Delete,
//            CreateOrUpdate
//        };
//        public void Initialize()
//        {
//            BaseTransformer = new DteTransformer();
//            BaseTransformer.CompressJson = CompressJson;
//            ServicePointManager.DefaultConnectionLimit = 100;
//            if (!PrependTableNames)
//            {
//                return;
//            }
//            var path = HostingEnvironment.ApplicationPhysicalPath;
//            var localInstance = path == null || !path.Contains("\\home\\site\\wwwroot\\");
//            if (localInstance)
//            {
//                _tablePrependString = Environment.MachineName;
//                var rgx = new Regex("[^a-zA-Z0-9]");
//                    _tablePrependString = rgx.Replace(_tablePrependString, "");
//                if (char.IsDigit(_tablePrependString[0])) //table names cannot start with a digit
//                    _tablePrependString = _tablePrependString.Substring(1);
//            }
//        }

//        private CloudStorageAccount StorageAccount
//        {
//            get
//            {
//                if (_storageAccount != null)
//                    return _storageAccount;

//                try
//                {
//                    _storageAccount = CloudStorageAccount.Parse(ConnectionString);
//                }
//                catch (StorageException e)
//                {
//                    throw new PersistenceException("Error accessing storage account.", e);
//                }

//                return _storageAccount;
//            }
//        }

//        private CloudTableClient _tableClient;
//        private CloudTableClient TableClient
//        {
//            get
//            {
//                try
//                {
//                    if (_tableClient != null)
//                        return _tableClient;
//                    var tableServicePoint = ServicePointManager.FindServicePoint(StorageAccount.TableEndpoint);
//                    tableServicePoint.UseNagleAlgorithm = false;
//                    tableServicePoint.Expect100Continue = false;
//                    return _tableClient = StorageAccount.CreateCloudTableClient();
//                }
//                catch (StorageException e)
//                {
//                    throw new PersistenceException("Error accessing table client.", e);
//                }
//            }
//        }

//        private CloudTable GetTable(string typeName)
//        {
//            return TableClient.GetTableReference(_tablePrependString + typeName);
//        }

//        public override void Create(object obj)
//        {
//            try
//            {
//                RunInterceptors(obj as PersistentEntity);
//                var insertObject = TransformObject(obj);
//                var table = GetTable(obj.GetType().Name);
//                table.CreateIfNotExists();
//                table.Execute(TableOperation.Insert(insertObject));
//            }
//            catch (StorageException e)
//            {
//                throw new PersistenceException(String.Format("Error inserting {0} into table.", obj), e);
//            }

//        }

//        public override T CreateOrUpdate<T>(T obj)
//        {
//            try
//            {
//                var type = obj.GetType();
//                var findMethod = this.GetType().GetMethod("Find", new [] {typeof(object)}).MakeGenericMethod(type);
//                RunInterceptors(obj as PersistentEntity);
//                var entry = TransformObject(obj);
                
//                try
//                {
//                    var temp = findMethod.Invoke(this, new[] { obj });
//                    ((DynamicTableEntity)entry)["CreatedDate"] = ((PersistentEntity)temp).CreatedDate.GetEntityProperty("CreatedDate");
//                }
//                catch (Exception e)
//                {
//                    if (String.Equals(e.InnerException.Message, "no entries match that pair of keys.", StringComparison.Ordinal))
//                    {
//                        Create(obj);
//                        return obj;
//                    }
//                    throw;
//                }

//                var table = GetTable(type.Name);
//                table.CreateIfNotExists();
//                table.Execute(TableOperation.InsertOrReplace(entry));
//                return BaseTransformer.TransformFromAzureObject<T>((DynamicTableEntity)entry, false);
//            }
//            catch (StorageException e)
//            {
//                throw new PersistenceException(String.Format("Error updating {0} in table.", obj), e);
//            }
//        }

//        public override void Update(object obj)
//        {
//            try
//            {
//                var type = obj.GetType();
//                var findMethod = this.GetType().GetMethod("Find", new[] { typeof(object) }).MakeGenericMethod(type);
//                RunInterceptors(obj as PersistentEntity);
//                var entry = TransformObject(obj);

//                try
//                {
//                    var temp = findMethod.Invoke(this, new[] {obj});
//                    ((DynamicPersistentEntity)entry)["CreatedDate"] = ((PersistentEntity)temp).CreatedDate;
//                    var table = GetTable(type.Name);
//                    table.Execute(TableOperation.Replace(entry));
//                }
//                catch(Exception e)
//                {
//                    if (String.Equals(e.InnerException.Message, "no entries match that pair of keys.", StringComparison.Ordinal))
//                    {
//                        throw new PersistenceException("No entry with those keys exists to update", e);
//                    }
//                    throw;
//                }
//            }
//            catch (StorageException e)
//            {
//                throw new PersistenceException("Error replacing entry, external modification made since entry was retrieved.", e);
//            }
            
//            catch (Exception e)
//            {
//                throw new PersistenceException(String.Format("Error updating {0} in table.", obj), e);
//            }

//        }

//        public override void Delete(object obj)
//        {
//            try
//            {
//                RunInterceptors(obj as PersistentEntity);
//                var table = GetTable(obj.GetType().Name);
//                var entry = TransformObject(obj);
//                TableOperation retrieveOperation = TableOperation.Retrieve<DynamicPersistentEntity>(entry.PartitionKey,
//                    entry.RowKey);
//                TableResult retrievedResult = table.Execute(retrieveOperation);
//                var deleteEntry = (DynamicPersistentEntity) retrievedResult.Result;

//                if (deleteEntry != null)
//                {
//                    TableOperation deleteOperation = TableOperation.Delete(deleteEntry);
//                    table.Execute(deleteOperation);
//                }
//                else
//                {
//                    throw new PersistenceException(String.Format("Object not found in table {0}", obj.GetType().Name));
//                }
//            }
//            catch (StorageException e)
//            {
//                throw new PersistenceException(String.Format("Error deleting object from table {0}", obj.GetType().Name), e);
//            }
//        }

//        //continuation code modeled off sample from http://convective.wordpress.com/2013/11/03/queries-in-the-windows-azure-storage-client--v2-1/
//        public override IList<T> FindAll<T>(IList<TableQueryParameters> parameters = null, bool preserveDateTimeKind = false) 
//        {
//            try
//            {
//                var updatedParameters = ProcessStartsWith(parameters);
//                var table = GetTable(typeof(T).Name);
//                if (!table.Exists())
//                    return new List<T>();

//                TableQuery<DynamicTableEntity> query = GenerateTableQuery<T>(table, updatedParameters);
//                var sw = new Stopwatch();
//                sw.Start();
//                TableContinuationToken continuationToken = null;
//                var returnList = new List<T>();
//                var convertElapsed = new Stopwatch();
//                do
//                {
//                    var queryResult = query.ExecuteSegmented(continuationToken);

//                    foreach (var item in queryResult)
//                    {
//                        convertElapsed.Start();
//                        returnList.Add(BaseTransformer.TransformFromAzureObject<T>(item, preserveDateTimeKind));
//                        convertElapsed.Stop();
//                    }
//                    //returnList.AddRange(queryResult.Select(entity => BaseTransformer.TransformFromAzureObject<T>(entity, preserveDateTimeKind)));

//                    continuationToken = queryResult.ContinuationToken;
//                } while (continuationToken != null);
//                sw.Stop();
//                Log.InfoFormat("{0} items took a total of {1} to retrieve from ATS.", returnList.Count, sw.Elapsed);
//                Log.InfoFormat("Took {0} to convert methods from base persistent entities",convertElapsed.Elapsed);
//                return returnList;
//            }
//            catch(StorageException e)
//            {
//                throw new PersistenceException(String.Format("Error finding all values in {0} table.",typeof (T).Name), e);
//            }
//        }

//        /**Use only if you have the exact partition key, not partial.
//         */
//        //continuation code modeled off sample from http://convective.wordpress.com/2013/11/03/queries-in-the-windows-azure-storage-client--v2-1/
//        public override IList<T> FindAll<T>(string partitionKey, IList<TableQueryParameters> parameters = null, IList<string> columns = null, bool preserveDateTimeKind = false)
//        {
//            try
//            {
//                var updatedParameters = ProcessStartsWith(parameters);
//                var table = GetTable(typeof(T).Name);
//                if (!table.Exists())
//                    return new List<T>();
//                if (updatedParameters == null)
//                {
//                    updatedParameters = new List<TableQueryParameters>();
//                }
//                updatedParameters.Add(new TableQueryParameters { PropertyName = "PartitionKey", Value = partitionKey, Comparator = TableQueryParameters.Comparators.EqualTo });
//                TableQuery<DynamicTableEntity> query = GenerateTableQuery<T>(table, updatedParameters, columns);
//                TableContinuationToken continuationToken = null;
//                var returnList = new List<T>();
//                var sw = new Stopwatch();
//                var times = new List<TimeSpan>();
//                do
//                {
//                    var queryResult = columns == null ? query.ExecuteSegmented(continuationToken) : table.ExecuteQuerySegmented(query, continuationToken);
//                    sw.Restart();
//                    returnList.AddRange(queryResult.Select(entity => BaseTransformer.TransformFromAzureObject<T>(entity, preserveDateTimeKind)));
//                    sw.Stop();
//                    times.Add(sw.Elapsed);
//                    continuationToken = queryResult.ContinuationToken;
//                } while (continuationToken != null);
//                var total = new TimeSpan();
//                total = times.Aggregate(total, (current, timeSpan) => current.Add(timeSpan));
//                Log.InfoFormat("{0} items took a total of {1} to retrieve from ATS.",returnList.Count,total);
//                return returnList;
//            }
//            catch (StorageException e)
//            {
//                throw new PersistenceException(String.Format("Error finding values with partition key {0} in {1} table.", partitionKey, typeof(T).Name), e);
//            }
//        }

//        public override T Find<T>(object obj)
//        {
//            return Find<T>(obj, false);
//        }
//        /**
//         * obj must be of base type PersistentEntity
//         */
//        public override T Find<T>(object obj, bool preserveDateTimeKind)
//        {
//            try
//            {
//                RunInterceptors(obj as PersistentEntity);
//                var entry = TransformObject(obj);
                
//                try
//                {
//                    var table = GetTable(typeof(T).Name);
//                    TableOperation retrieveOperation = TableOperation.Retrieve<DynamicTableEntity>(entry.PartitionKey,entry.RowKey);
//                    TableResult result = table.Execute(retrieveOperation);
//                    if (result.Result != null)
//                    {
//                        return
//                            BaseTransformer.TransformFromAzureObject<T>((DynamicTableEntity)result.Result, preserveDateTimeKind);
//                    }
//                    else
//                    {
//                        throw new PersistenceException("no entries match that pair of keys.");
//                    }
//                }
//                catch (StorageException e)
//                {
//                    throw new PersistenceException(
//                        String.Format("Error finding entry with partition key {0} and row key {1}.", entry.PartitionKey,
//                            entry.RowKey), e);
//                }
//            }
//            catch (StorageException e)
//            {
//                throw new PersistenceException("invalid parameter: obj must be of base type PersistentEntity.", e);
//            }
            
//        }

//        /**
//         * all objects will be added whether they are currently in the table or not.
//         */
//        public override void BatchCreate<T>(IList<T> objects, TimeSpan? preProcessingTime = null)
//        {
//            RunBatch(objects, BatchOperations.Create, preProcessingTime);
//        }

//        /**
//         * all objects must be currently present in the table.
//         */
//        public override void BatchUpdate<T>(IList<T> objects, TimeSpan? preProcessingTime = null)
//        {
//            RunBatch(objects, BatchOperations.Update, preProcessingTime);
//        }

//        public override void BatchCreateOrUpdate<T>(IList<T> objects, TimeSpan? preProcessingTime = null)
//        {
//            RunBatch(objects, BatchOperations.CreateOrUpdate, preProcessingTime);
//        }

//        /**
//         * all objects must be currently present in the table.
//         */
//        public override void BatchDelete<T>(IList<T> objects, TimeSpan? preProcessingTime = null)
//        {
//            RunBatch(objects,BatchOperations.Delete, preProcessingTime);
//        }


//        private void RunBatch<T>(IList<T> objects, BatchOperations operation, TimeSpan? preProcessingTime = null) where T : PersistentEntity
//        {
//            try
//            {
//                if (objects.Count == 0)
//                    return;
//                var totalSw = new Stopwatch();
//                totalSw.Start();

//                var table = GetTable(typeof(T).Name);
//                switch (operation)
//                {
//                    case BatchOperations.Create:
//                        table.CreateIfNotExists();
//                        break;
//                    case BatchOperations.CreateOrUpdate:
//                        table.CreateIfNotExists();
//                        break;
//                    case BatchOperations.Update:
//                        if (!table.Exists())
//                            throw new PersistenceException("Can not update entries, table does not exist.");
//                        break;
//                    case BatchOperations.Delete:
//                        if (!table.Exists())
//                            throw new PersistenceException("Can not delete entries, table does not exist.");
//                        break;
//                    default:
//                        throw new PersistenceException("Invalid batch operation specified.");
//                }

//                var partitionedList = PartitionList(objects, Batchsize);
//                var times = new ConcurrentBag<KeyValuePair<int,TimeSpan>>();

//                Parallel.ForEach(partitionedList, a => RunSingleBatchOperation(table, a, operation, times));
                
//                totalSw.Stop();
//                var minTime = times.OrderBy(t => t.Value).FirstOrDefault();
//                var maxTime = times.OrderByDescending(t => t.Value).FirstOrDefault();
//                var batchSizes = times.Select(t => t.Key).ToList();
//                var batchTimes = times.Select(t => t.Value).ToList();
                
//                Log.InfoFormat("Minimum time to {2} batch ({0} entries): {1}", minTime.Key, minTime.Value, operation);
//                Log.InfoFormat("Maximum time to {2} batch ({0} entries): {1}", maxTime.Key, maxTime.Value, operation);
//                Log.InfoFormat("Batch sizes ranged from {0} to {1} with an average of {2}",batchSizes.Min(),batchSizes.Max(), batchSizes.Average());
//                Log.InfoFormat("{0} times ranged from {1} to {2} with an average of {3} sec",operation, batchTimes.Min(), batchTimes.Max(), batchTimes.Average(t => t.TotalSeconds));
//                Log.InfoFormat("Total elapsed time to {2} {1} entries ({3} batches): {0}", totalSw.Elapsed, objects.Count, operation, partitionedList.Count);

//                if(preProcessingTime != null)
//                    Log.InfoFormat("Total time including pre processing: {0}",totalSw.Elapsed + preProcessingTime);
//            }
//            catch (PersistenceException)
//            {
//                throw;
//            }
//            catch (Exception ex)
//            {
//                throw new PersistenceException(string.Format("Error during batch {0}: {1}",operation, ex));
//            }
//        }
//        //TODO: add result filter option for queries

//        private void RunSingleBatchOperation<T>(CloudTable table, List<T> list, BatchOperations operation, ConcurrentBag<KeyValuePair<int,TimeSpan>> times) where T : PersistentEntity
//        {
//            var batchOperation = new TableBatchOperation();
//            foreach (var obj in list)
//            {
//                RunInterceptors(obj);
//                switch (operation)
//                {
//                    case BatchOperations.Create:
//                        batchOperation.Insert(TransformObject(obj));
//                        break;
//                    case BatchOperations.CreateOrUpdate:
//                        batchOperation.InsertOrReplace(TransformObject(obj));
//                        break;
//                    case BatchOperations.Update:
//                        batchOperation.Replace(TransformObject(obj));
//                        break;
//                    case BatchOperations.Delete:
//                        var deleteObject = TransformObject(obj);
//                        deleteObject.ETag = "*";
//                        batchOperation.Delete(deleteObject);
//                        break;
//                    default:
//                        throw new PersistenceException("Invalid batch operation specified.");
//                }
//            }
//            try
//            {
//                var sw = Stopwatch.StartNew();
//                table.ExecuteBatch(batchOperation);
//                sw.Stop();
//                times.Add(new KeyValuePair<int, TimeSpan>(list.Count, sw.Elapsed));
//                //Log.WarnFormat("Total elapsed time to execute batch {2} with {1} entries: {0}", sw.Elapsed, list.Count, operation);
//            }
//            catch (StorageException ex)
//            {
//                if (ex.Message.StartsWith("Unexpected response code for operation : "))
//                {
//                    var index = int.Parse(ex.Message.Substring(41));
//                    if (index <= list.Count)
//                    {
//                        T obj;
//                        if (index > 0)
//                        {
//                            obj = list[index - 1];
//                        }
//                        else if (index < list.Count && list.Count != 1)
//                            obj = list[index + 1];
//                        else
//                        {
//                            obj = null;
//                        }

//                        if (obj != null && ((obj).RowKey == null || (obj).PartitionKey == null))
//                        {
//                            RunInterceptors(obj);
//                        }

//                        var prevObj = obj == null ? null : TransformObject(obj) as DynamicTableEntity;

//                        var errorObj = list[index];

//                        if (errorObj.RowKey == null || errorObj.PartitionKey == null)
//                            RunInterceptors(errorObj);
//                        var errorTableObj = TransformObject(errorObj) as DynamicTableEntity;
//                        LogBatchError(string.Format("Error in batch {1} with entry {0}:", index, operation), prevObj, errorTableObj);
//                    }
//                }
//                throw new PersistenceException("Error during batch update: ", ex);
//            }
//        }

//        private void LogBatchError(string message, DynamicTableEntity prevObj, DynamicTableEntity errorObj)
//        {
//            Log.ErrorFormat("Batch Log Message {0}",message);

//            if (prevObj != null)
//            {
//                var properties = prevObj.Properties;
//                Log.ErrorFormat("\nPrevious Object:");
//                Log.ErrorFormat("PartitionKey: {0}",prevObj.PartitionKey);
//                Log.ErrorFormat("RowKey: {0}",prevObj.RowKey);
//                foreach (var property in properties)
//                {
//                    var value = property.Value.PropertyAsObject;
//                    Log.ErrorFormat("{0}: {1}", property.Key, value);
//                }
//            }
//            var errorProperties = errorObj.Properties;
//            Log.ErrorFormat("\nError Object:");
//            Log.ErrorFormat("PartitionKey: {0}", errorObj.PartitionKey);
//            Log.ErrorFormat("RowKey: {0}", errorObj.RowKey);
//            foreach (var property in errorProperties)
//            {
//                var value = property.Value.PropertyAsObject;
//                Log.ErrorFormat("{0}: {1}", property.Key, value);
//            }
//        }

//        #region entry initialization
//        private ITableEntity TransformObject(object obj)
//        {
//            try
//            {
//                var o = obj as ITableEntity;
//                if (o != null)
//                {
//                    return o;
//                }
//                var entity = obj as PersistentEntity;
//                if (entity != null)
//                    return BaseTransformer.TransformToAzureObject((PersistentEntity)obj);
//            }
//            catch(Exception ex) { Log.ErrorFormat("Error Checking Object {0}",ex); throw new InvalidDataException(string.Format("Object {0} does not implement ITableEntity and cannot be stored in Azure Table Store", obj.GetType().Name));}
//            return null;
//        }

//        private bool RunInterceptors(PersistentEntity entity)
//        {
//            try
//            {
//                if (UpdateInterceptor == null)
//                    UpdateInterceptor += Interceptor.OnTableInteraction;
//                EventHandler<PersistentEntity> handler = UpdateInterceptor;
//                if (handler != null)
//                {
//                    handler(null, entity);
//                    return true;
//                }
//                return false;
//            }
//            catch (Exception ex)
//            {
//                Log.ErrorFormat("Error Finalizing Object {0}",ex);
//                throw;
//            }
//        }
//        #endregion

//        #region query generation support
//        private TableQuery<DynamicTableEntity> GenerateTableQuery<T>(CloudTable table, IList<TableQueryParameters> parameters = null, IList<string> columns = null) where T : class
//        {
//            if (parameters != null)
//            {
//                var values = parameters.Select(p => p.Value).ToList();
//                double val = 100;
//                if(columns == null)
//                    return (from entity in table.CreateQuery<DynamicTableEntity>() select entity).Where(GenerateIQueryableWhereString<T>(parameters)).AsTableQuery<DynamicTableEntity>();
//                else
//                {
//                    return new TableQuery<DynamicTableEntity>().Where(GenerateFluentWhereString<T>(parameters)).Select(columns).AsTableQuery<DynamicTableEntity>();
//                }
//            }
//            else
//            {
//                return (from entity in table.CreateQuery<DynamicTableEntity>() select entity).AsTableQuery<DynamicTableEntity>();
//            }
//        }

//        //IQueryable is the newer type of query, however it does not allow for linq type .Select
//        #region IQueryable
//        private string GenerateIQueryableWhereString<T>(IList<TableQueryParameters> parameters)
//        {
//            string whereString = SingleIQueryableComparisonString<T>(parameters[0]);
//            for(int i = 1; i < parameters.Count; i++)
//            {
//                whereString = whereString + " and " + SingleIQueryableComparisonString<T>(parameters[i]);
//            }
//            return whereString;
//        }

//        private string SingleIQueryableComparisonString<T>(TableQueryParameters parameters)
//        {
//            string comparisonString;
//            var baseProperties = new string[] {"PartitionKey", "RowKey", "ETag"};
//            if(baseProperties.Any(p => p.Equals(parameters.PropertyName)))
//                comparisonString = parameters.PropertyName + ExpressionString(parameters) + "\"" + parameters.Value + "\"";
//            else
//            {
//                comparisonString = "Properties[\"" + parameters.PropertyName + "\"]" + TypeSelect<T>(parameters);
//            }
//            return comparisonString;
//        }

//        private string TypeSelect<T>(TableQueryParameters parameters)
//        {
//            var obj = Activator.CreateInstance<T>();
//            var properties = obj.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
//            PropertyInfo property;
//            try
//            {
//                property = properties.First(p => p.Name == parameters.PropertyName);
//            }
//            catch (Exception ex)
//            {
//                Log.ErrorFormat("Error attempting to find property {0} on object {1}: {2}",parameters.PropertyName, obj.GetType().Name, ex);
//                throw;
//            }
//            var propertyType = property.PropertyType;

//            if (propertyType == typeof(String))
//                return ".StringValue" + ExpressionString(parameters) + "\"" + parameters.Value + "\"";

//            if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
//                return ".DateTime" + ExpressionString(parameters) + "DateTime.Parse(\"" + parameters.Value + "\")";

//            if (propertyType == typeof(byte[]))
//                return ".BinaryValue" + ExpressionString(parameters) + parameters.Value;

//            if (propertyType == typeof(Double) || propertyType == typeof(Double?))
//                return ".DoubleValue" + ExpressionString(parameters) + parameters.Value;

//            if (propertyType == typeof(Decimal) || propertyType == typeof(Decimal?))
//                return ".DoubleValue" + ExpressionString(parameters) + parameters.Value;

//            if (propertyType == typeof(Int32) || propertyType == typeof(Int32?))
//                return ".Int32Value" + ExpressionString(parameters) + parameters.Value;

//            if (propertyType == typeof(Int64) || propertyType == typeof(Int64?))
//                return ".Int64Value" + ExpressionString(parameters) + parameters.Value;

//            if (propertyType == typeof(Guid) || propertyType == typeof(Guid?))
//                return ".GuidValue" + ExpressionString(parameters) + "Guid.Parse(\"" + parameters.Value + "\")";
//            return null;
//        }
//        #endregion

//        //need to use Fluent queries in order to flexibly restrict return values.
//        #region Fluent
//        private string GenerateFluentWhereString<T>(IList<TableQueryParameters> parameters)
//        {
//            string whereString = SingleFluentComparisonString<T>(parameters[0]);
//            for (int i = 1; i < parameters.Count; i++)
//            {
//                whereString = whereString + " and " + SingleFluentComparisonString<T>(parameters[i]);
//            }
//            return whereString;
//        }

//        private string SingleFluentComparisonString<T>(TableQueryParameters parameters)
//        {
//            var obj = Activator.CreateInstance<T>();
//            var properties = obj.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
//            var property = properties.First(p => p.Name == parameters.PropertyName);
//            var propertyType = property.PropertyType;

//            if (propertyType == typeof(String))
//                return TableQuery.GenerateFilterCondition(parameters.PropertyName,ExpressionString(parameters,true),parameters.Value.ToString());

//            if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
//                return TableQuery.GenerateFilterConditionForDate(parameters.PropertyName, ExpressionString(parameters, true), (DateTime)parameters.Value);

//            if (propertyType == typeof(byte[]))
//                return TableQuery.GenerateFilterConditionForBinary(parameters.PropertyName, ExpressionString(parameters, true), (byte[])parameters.Value);

//            if (propertyType == typeof(Double) || propertyType == typeof(Double?))
//                return TableQuery.GenerateFilterConditionForDouble(parameters.PropertyName, ExpressionString(parameters, true), (double)parameters.Value);

//            if (propertyType == typeof(Decimal) || propertyType == typeof(Decimal?))
//                return TableQuery.GenerateFilterConditionForDouble(parameters.PropertyName, ExpressionString(parameters, true), (double)parameters.Value);

//            if (propertyType == typeof(Int32) || propertyType == typeof(Int32?))
//                return TableQuery.GenerateFilterConditionForInt(parameters.PropertyName, ExpressionString(parameters, true), (int)parameters.Value);

//            if (propertyType == typeof(Int64) || propertyType == typeof(Int64?))
//                return TableQuery.GenerateFilterConditionForLong(parameters.PropertyName, ExpressionString(parameters, true), (long)parameters.Value);

//            if (propertyType == typeof(Guid) || propertyType == typeof(Guid?))
//                return TableQuery.GenerateFilterConditionForGuid(parameters.PropertyName, ExpressionString(parameters, true), (Guid)parameters.Value);
//            return null;
//        }
//        #endregion

//        private string ExpressionString(TableQueryParameters parameters, bool isFluent = false)
//        {
//            switch (parameters.Comparator)
//            {
//                case TableQueryParameters.Comparators.EqualTo:
//                    return isFluent ? " eq " : " == ";
//                case TableQueryParameters.Comparators.LessThan:
//                    return isFluent ? " lt " : " < ";
//                case TableQueryParameters.Comparators.LessThanOrEqualTo:
//                    return isFluent ? " le " : " <= ";
//                case TableQueryParameters.Comparators.GreaterThan:
//                    return isFluent ? " gt " : " > ";
//                case TableQueryParameters.Comparators.GreaterThanOrEqualTo:
//                    return isFluent ? " ge " : " >= ";
//                default:
//                    return isFluent ? " eq " : " == ";
//            }
//        }

//        #endregion

//        #region batch preparation
//        private List<List<T>> PartitionList<T>(IList<T> list, int partitionSize) where T : PersistentEntity
//        {
//            if (list == null)
//                throw new ArgumentNullException("list");

//            if (partitionSize < 1 || partitionSize > 100)
//                throw new ArgumentOutOfRangeException("partitionSize");

//            try
//            {
//                var keySortedList = list.GroupBy(item => item.PartitionKey);

//                var allPartitions = new List<List<T>>();
//                int listNum = 0;
//                foreach (var groupByKey in keySortedList)
//                {
//                    var listByKey = groupByKey.ToList();
//                    var partitionCount = (int) Math.Ceiling(listByKey.Count()/(double) partitionSize);
//                    var totalCount = listByKey.Count();

//                    int k = 0;
//                    for (int i = 0; i < partitionCount; i++)
//                    {
//                        allPartitions.Add(new List<T>(partitionSize));
//                        for (int j = k; j < k + partitionSize; j++)
//                        {
//                            if (j >= totalCount)
//                                break;
//                            allPartitions[listNum].Add(listByKey[j]);
//                        }
//                        k += partitionSize;
//                        listNum++;
//                    }
//                }
//                return allPartitions;
//            }
//            catch (Exception ex)
//            {
//                Log.ErrorFormat("Error partitioning list {0}",ex);
//                throw;
//            }
//        }

//        private IList<TableQueryParameters> ProcessStartsWith(IList<TableQueryParameters> parameters)
//        {
//            if (parameters == null || parameters.Count == 0)
//                return null;

//            var updatedParams = new List<TableQueryParameters>();
//            foreach (var parameter in parameters)
//            {
//                if (parameter.Comparator == TableQueryParameters.Comparators.StartsWith)
//                {
//                    if (!(parameter.Value is string))
//                        continue;

//                    var value = parameter.Value.ToString();
//                    var length = value.Length;
//                    var lastChar = value[length - 1];
//                    var newLastChar = (char) (lastChar + 1);
//                    var upperBound = value.Substring(0, length - 1) + newLastChar;

//                    updatedParams.Add(new TableQueryParameters
//                    {
//                        Comparator = TableQueryParameters.Comparators.GreaterThanOrEqualTo,
//                        PropertyName = parameter.PropertyName,
//                        Value = parameter.Value
//                    });
                    
//                    updatedParams.Add(new TableQueryParameters
//                    {
//                        Comparator = TableQueryParameters.Comparators.LessThan,
//                        PropertyName = parameter.PropertyName,
//                        Value = upperBound
//                    });
//                }
//                else
//                {
//                    updatedParams.Add(new TableQueryParameters
//                    {
//                        Comparator = parameter.Comparator,
//                        PropertyName = parameter.PropertyName,
//                        Value = parameter.Value
//                    });
//                }
//            }
//            return updatedParams;
//        }
//        #endregion

//        /**
//         * For testing purposes only 
//         */
//        public override IEnumerable<T> TakeFive<T>()
//        {
//            try
//            {
//                var table = GetTable(typeof(T).Name);
//                var query = (from entity in table.CreateQuery<DynamicTableEntity>() select entity).Take(5).AsTableQuery();
//                var result = query.Execute();
//                if (result != null)
//                {
//                    return
//                        result.Select(entry => BaseTransformer.TransformFromAzureObject<T>((DynamicTableEntity)entry, false));
//                }
//                return null;
//            }
//            catch (StorageException e)
//            {
//                throw new PersistenceException(
//                    String.Format("Error finding 5 elements"), e);
//            }
//        }
//    }

//}
