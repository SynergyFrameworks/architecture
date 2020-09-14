using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace MML.Enterprise.Persistence
{
    public interface IKeyValueManager
    {
        void CreateOrUpdate(string key, Stream value);
        IList<string> FindAllKeys();
        Stream GetValue(string key);
        IList<Stream> GetValues(IList<string> keys);
        IList<string> FindKeysById(string id);
        void Delete(string key);
    }
}
