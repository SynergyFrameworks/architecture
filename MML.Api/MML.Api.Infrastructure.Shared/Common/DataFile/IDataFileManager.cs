using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MML.Enterprise.Common.Mapping;

namespace MML.Enterprise.Common.DataFile
{
    /// <summary>
    /// DataFile Managers are the abstraction wrapper for all file I/O.
    /// Its main responsibilities are: 
    /// 1) The management/discovery of appropriate mappings
    /// 2) The management of transformers, and choosing which transformer to use. 
    /// </summary>
    public interface IDataFileManager
    {
        IDictionary<string, IDataFileTransformer> Transformers { get; set; }
        IDictionary<string, IMappingProvider> MappingProviders { get; set; }
        Stream TransformToFile(string format, Mapping.Mapping mapping, IList<Dictionary<string, object>> data);
        Stream TransformToFile(string format, string mapping, IList<Dictionary<string, object>> data);
        Stream TransformToFile(string format, Mapping.Mapping mapping, IList<Dictionary<string, object>> data, IDictionary<string, object> parameters);
        Stream TransformToFile(string format, IList data, IList<string> properties = null);

        List<Mapping.Mapping> Mappings { get; set; }
        Dictionary<string, object> TransformFromFile(string format, string mapping, Stream file);
        Dictionary<string, object> TransformFromFile(string format, Mapping.Mapping mapping, Stream file);

        IList<T> TransformFromFile<T>(string format, Mapping.Mapping mapping, Stream file);

        Mapping.Mapping GenerateMapping(string format, FileInfo file, IDictionary<string, object> parameters = null);
        Mapping.Mapping GenerateMapping(string format, Stream file, IDictionary<string, object> parameters = null);
    }
}
