//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using Microsoft.WindowsAzure.Storage;
//using Microsoft.WindowsAzure.Storage.Table;
//using MML.Enterprise.Persistence.Azure.Transformers;
//using MML.Enterprise.Persistence.Azure.ErrorHandlers;
//using log4net;

//namespace MML.Enterprise.Persistence.Azure
//{
//    public class AzureEntityManager : IAzureEntityManager
//    {
//       // private ILog Log = LogManager.GetLogger<AzureEntityManager>();
//        public string ConnectionString { get; set; }
//        public IDictionary<string, IAzureTransformer> Transformers { get; set; }
//        public IDictionary<string, IErrorHandler> ErrorHandlers { get; set; }
//        public string DefaultTransformerName { get; set; }
//        public string DefaultErrorHandlerName { get; set; }
//        private CloudStorageAccount _storageAccount;

//        private CloudStorageAccount StorageAccount
//        {
//            get
//            {
//                if (_storageAccount == null)
//                {
//                    try
//                    {
//                        _storageAccount = CloudStorageAccount.Parse(ConnectionString);
//                    }
//                    catch (Exception Ex)
//                    {
//                     ///   Log.ErrorFormat("Error parsing connection string {0}", Ex);
//                        throw Ex;
//                    }
//                }
//                return _storageAccount;
//            }
//        }

//        private CloudTableClient _tableClient;
//        private CloudTableClient TableClient
//        {
//            get
//            {
//                if (_tableClient == null)
//                    _tableClient = StorageAccount.CreateCloudTableClient();
//                return _tableClient;
//            }
//        }

//        public void Create(PersistentEntity obj)
//        {

//        // Log.Info(m => m("Creating type {0}, ClientKey {1}, RowKey{2}", obj.GetType().Name, obj.PartitionKey, obj.Id));
//            var typeName = obj.GetType().Name;
//            var entity = GetTransformer(typeName).TransformToAzureObject(obj);
//            var table = TableClient.GetTableReference(typeName);
//            try
//            {
//                table.CreateIfNotExists();
//                table.Execute(TableOperation.Insert(entity));
//            }
//            catch (Exception ex)
//            {
//              //  Log.ErrorFormat("Error Creating type {0}, ClientKey {1}, RowKey{2}. Exception: {3}", obj.GetType().Name, obj.PartitionKey, obj.Id, ex);
//                var errorHandler = GetErrorHandler(typeName);
//                ex = errorHandler.HandleError(ex);
//                if (ex != null)
//                    throw ex;
//            }
//        }



//        public void Update(PersistentEntity obj)
//        {
//           // Log.Info(m => m("Creating type {0}, ClientKey {1}, RowKey{2}", obj.GetType().Name, obj.PartitionKey, obj.Id));
//            var typeName = obj.GetType().Name;
//            var entity = GetTransformer(typeName).TransformToAzureObject(obj);
//            var findMethod = this.GetType().GetMethod("Find").MakeGenericMethod(obj.GetType());
//            var existing = findMethod.Invoke(this, null);
//            try
//            {
//                if (existing == null)
//                    Create(obj);
//                var table = TableClient.GetTableReference(typeName);
//                table.ExecuteAsync(TableOperation.Replace(entity));
//            }
//            catch (Exception ex)
//            {
//                //Log.ErrorFormat("Error Updating type {0}, ClientKey {1}, RowKey{2}. Exception: {3}", obj.GetType().Name, obj.PartitionKey, obj.Id, ex);
//                var errorHandler = GetErrorHandler(typeName);
//                ex = errorHandler.HandleError(ex);
//                if (ex != null)
//                    throw ex;
//            }

//        }

//        public void Delete(string container, string clientId, string objectId)
//        {
//            var table = TableClient.GetTableReference(container);
//            var temp = new DynamicPersistentEntity { PartitionKey = clientId, RowKey = objectId, ETag = "*" };
//            table.ExecuteAsync(TableOperation.Delete(temp));
//        }

//        public void Delete<T>(string clientId, string objectId)
//        {
//            Delete(typeof(T).Name, clientId, objectId);
//        }

//        //public IList<T> FindAll<T>() where T : PersistentEntity
//        //{
//        // //   Log.Info(m => m("Finding All for type {0}", typeof(T).Name));
//        //    var table = TableClient.GetTableReference(typeof(T).Name);
//        //    var items = table.ExecuteQuery(new TableQuery<DynamicPersistentEntity>()).ToList();
//        //    var transformer = GetTransformer(typeof(T).Name);
//        //    try
//        //    {
//        //        return items.Select(i => transformer.TransformFromAzureObject<T>(i)).Cast<T>().ToList();
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //      //  Log.ErrorFormat("Error Finding All for type {0}. {1}", typeof(T).Name, ex);
//        //        var errorHandler = GetErrorHandler(typeof(T).Name);
//        //        ex = errorHandler.HandleError(ex);
//        //        if (ex != null)
//        //            throw ex;
//        //    }
//        //    return null;
//        //}

//        //public T Find<T>(string clientId, string rowId)
//        //{
//        //  //  Log.Info(m => m("Finding All for type {0}, ClientKey {1}", typeof(T).Name, clientId));
//        //    var table = TableClient.GetTableReference(typeof(T).Name);
//        //    var transformer = GetTransformer(typeof(T).Name);
//        //    var query =
//        //        new TableQuery<DynamicPersistentEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey",
//        //                                                                                           QueryComparisons
//        //                                                                                               .Equal, clientId)).Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowId));
//        //    try
//        //    {
//        //        //var results = table.ExecuteQuery(query).Select(i => transformer.TransformFromAzureObject<T>(i)).ToList();
//        //        //return results.FirstOrDefault();
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //     //   Log.ErrorFormat("Error Finding All for type {0}, ClientKey {1}. {2}", typeof(T).Name, clientId, ex);
//        //        var errorHandler = GetErrorHandler(typeof(T).Name);
//        //        ex = errorHandler.HandleError(ex);
//        //        if (ex != null)
//        //            throw ex;
//        //    }
//        //   // throw new InstanceNotFoundException();
//        //}

//        public IList<T> FindAllByClient<T>(string clientId) where T : PersistentEntity
//        {
//       //     Log.Info(m => m("Finding All for type {0}, ClientKey {1}", typeof(T).Name, clientId));
//            var table = TableClient.GetTableReference(typeof(T).Name);
//            var transformer = GetTransformer(typeof(T).Name);
//            var query =
//                new TableQuery<DynamicPersistentEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey",
//                                                                                                   QueryComparisons
//                                                                                                       .Equal, clientId));
//            try
//            {
//               // return table.ExecuteQuery(query).Select(i => transformer.TransformFromAzureObject<T>(i)).ToList();
//            }
//            catch (Exception ex)
//            {
//               // Log.ErrorFormat("Error Finding All for type {0}, ClientKey {1}. {2}", typeof(T).Name, clientId, ex);
//                var errorHandler = GetErrorHandler(typeof(T).Name);
//                ex = errorHandler.HandleError(ex);
//                if (ex != null)
//                    throw ex;
//            }
//            return null;
//        }

//        public IList<TC> FindAllCollectionByType<TP, TC>(string clientId, string parentId)
//        {
//            return FindAllCollectionByContainerName<TC>(typeof(TP).Name, clientId, parentId);
//        }



//        public void AddToCollection<T>(PersistentEntity obj)
//        {
//            AddToCollection(typeof(T).Name, obj);
//        }

//        public void AddToCollection(string container, PersistentEntity obj)
//        {
//            var table = TableClient.GetTableReference(container);
//            var transformer = GetTransformer(obj.GetType().Name);
//            table.ExecuteAsync(TableOperation.InsertOrMerge(transformer.TransformToAzureObject(obj)));

//        }

//        public IList<T> FindAllCollectionByContainerName<T>(string containerName, string clientId, string parentId)
//        {
//            var table = TableClient.GetTableReference(containerName);
//            var transformer = GetTransformer(typeof(T).Name);

//            //var query = (from e in table.CreateQuery<DynamicPersistentEntity>()
//            //             where e.PartitionKey == clientId
//            //                   && e.RowKey.CompareTo(parentId) > 0
//            //                   && e.RowKey.CompareTo(GetParentIdComparer(parentId)) <= 0
//            //             select e);
//            //var results = query.ToList();
//            //return results.Select(e => transformer.TransformFromAzureObject<T>(e)).ToList();
//        }

//        private string GetParentIdComparer(string parentId)
//        {
//            var array = parentId.ToArray();
//            //            array[array.Length - 1] = array[array.Length - 1]++;
//            array[array.Length - 1]++;
//            return new string(array);
//        }

//        private void CheckObject(object obj)
//        {
//            if (!(obj is ITableEntity))
//                throw new InvalidDataException(string.Format("Object {0} does not implement ITableEntity and cannot be stored in Azure Table Store", obj.GetType().Name));
//        }

//        private IAzureTransformer GetTransformer(string type)
//        {
//            return Transformers.ContainsKey(type) ? Transformers[type] : Transformers[DefaultTransformerName];
//        }

//        private IErrorHandler GetErrorHandler(string type)
//        {
//            return ErrorHandlers.ContainsKey(type) ? ErrorHandlers[type] : ErrorHandlers[DefaultErrorHandlerName];
//        }
//    }
//}
