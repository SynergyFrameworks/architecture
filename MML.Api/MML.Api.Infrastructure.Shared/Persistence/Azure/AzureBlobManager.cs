using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MML.Enterprise.Common.Persistence;

namespace MML.Enterprise.Persistence.Azure
{
    public class AzureBlobManager : IKeyValueManager
    {
        public string ConnectionString { get; set; }
        private CloudStorageAccount _storageAccount;
        public string ContainerName { get; set; }
        private CloudStorageAccount StorageAccount
        {
            get
            {
                if (_storageAccount == null)
                {
                    try
                    {
                        _storageAccount = CloudStorageAccount.Parse(ConnectionString);
                    }
                    catch (StorageException e)
                    {
                        throw new PersistenceException("Error accessing storage account.", e);
                    }
                }
                return _storageAccount;
            }
        }

        private CloudBlobClient _blobClient;
        private CloudBlobClient BlobClient
        {
            get
            {
                try
                {
                    return _blobClient ?? (_blobClient = StorageAccount.CreateCloudBlobClient());
                }
                catch (StorageException e)
                {
                    throw new PersistenceException("Error accessing blob client.", e);
                }
            }
        }

        private CloudBlobContainer _container;
        public CloudBlobContainer Container
        {
            get
            {
                try
                {
                    if (_container != null)
                        return _container;

                    _container = BlobClient.GetContainerReference(ContainerName);
                    _container.CreateIfNotExistsAsync();
                    return _container;
                }
                catch (StorageException e)
                {
                    throw new PersistenceException("Error accessing storage container.", e);
                }
            }
        }

        public void CreateOrUpdate(string key,Stream  value)
        {
            try
            {
                var reference = Container.GetBlockBlobReference(key);
                reference.UploadFromStreamAsync(value);
            }
            catch (StorageException e)
            {
                throw new PersistenceException(String.Format("Error updating blob {0} in container {1}",key,Container), e);
            }
        }

        //public IList<string> FindAllKeys()
        //{
        //    try { 
        //       // return (from CloudBlockBlob blob in Container.ListBlobsSegmentedAsync(null, true) select blob.Uri.ToString()).ToList();
        //    }
        //    catch (StorageException e)
        //    {
        //        throw new PersistenceException(String.Format("Error accessing blobs in container {0}", Container), e);
        //    }
        //}

        public IList<string> FindKeysById(string id)
        {
            try
            {
                var keys = new List<string>();
                //null or empty string, return any documents that do not have an id, global documents
                if (id == null || id.Equals("", StringComparison.Ordinal))
                {
                    //foreach (var item in Container.ListBlobs())
                    //{
                    //    if (item is CloudBlockBlob)
                    //    {
                    //        var blob = (CloudBlockBlob) item;
                    //        keys.Add(blob.Uri.ToString());
                    //    }
                    //    else if (item is CloudPageBlob)
                    //    {
                    //        var pageBlob = (CloudPageBlob) item;
                    //        keys.Add(pageBlob.Uri.ToString());
                    //    }
                    //}
                }
                else
                {
                    //foreach (var item in Container.ListBlobs())
                    //{
                    //    if (item is CloudBlobDirectory)
                    //    {
                    //        var blob = (CloudBlobDirectory) item;
                    //        var directorySplit = blob.Uri.ToString().Split(new[] {'/'});
                    //        if (directorySplit[directorySplit.Length - 2].Equals(id, StringComparison.Ordinal))
                    //        {
                    //            keys.AddRange(blob.ListBlobs().Select(listBlobItem => listBlobItem.Uri.ToString()));
                    //            break;
                    //        }
                    //    }
                  //  }
                }
                return keys;
            }
            catch (StorageException e)
            {
                throw new PersistenceException(String.Format("Error accessing blobs with prefix {0} in container {1}", id, Container), e);
            }
        }

        public Stream GetValue(string key)
        {
            try
            {
                var reference = Container.GetBlockBlobReference(key);
                var stream = new MemoryStream();
                //if(reference.Exists())
                //    reference.DownloadToStreamAsync(stream);
                return stream;
            }
            catch (StorageException e)
            {
                throw new PersistenceException(String.Format("Error accessing blob {0} in container {1}", key, Container), e);
            }
        }

        public IList<Stream> GetValues(IList<string> keys)
        {
            try
            {
                return keys.Select(GetValue).ToList();
            }
            catch (StorageException e)
            {
                throw new PersistenceException(String.Format("Error accessing blobs in container {0}", Container), e);
            }
        }

        public void Delete(string key)
        {
            try
            {
                CloudBlockBlob blockBlob = Container.GetBlockBlobReference(key);
                blockBlob.DeleteAsync();
            }
            catch (StorageException e)
            {
                throw new PersistenceException(String.Format("Error deleting blob {0} in container {1}", key, Container), e);
            }
        }

        public IList<string> FindAllKeys()
        {
            throw new NotImplementedException();
        }
    }
}

