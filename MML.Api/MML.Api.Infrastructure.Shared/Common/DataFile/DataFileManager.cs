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
    public class DataFileManager : IDataFileManager
    {
        public IDictionary<string, IDataFileTransformer> Transformers { get; set; }
        public IDictionary<string, IMappingProvider> MappingProviders { get; set; }


        public List<Mapping.Mapping> Mappings { get; set; }
        public void Initialize()
        {
            Mappings = new List<Mapping.Mapping>();
            foreach (var mappingProvider in MappingProviders)
            {
                Mappings.AddRange(mappingProvider.Value.GenerateMappings());
            }
        }
        public Stream TransformToFile(string format, Mapping.Mapping mapping, IList<Dictionary<string, object>> data)
        {
            if (!Transformers.ContainsKey(format))
                throw new NotSupportedException("This format is not supported");
            return Transformers[format].TransformToFile(mapping, data);
        }

        public Stream TransformToFile(string format, string mapping, IList<Dictionary<string, object>> data)
        {
            throw new NotImplementedException();
        }

        public Stream TransformToFile(string format, Mapping.Mapping mapping, IList<Dictionary<string, object>> data, IDictionary<string, object> parameters)
        {
            if (!Transformers.ContainsKey(format))
                throw new NotSupportedException("This format is not supported");
            return Transformers[format].TransformToFile(mapping, data, parameters);
        }

        public Stream TransformToFile(string format, IList data, IList<string> properties = null)
        {
            if (!Transformers.ContainsKey(format))
                throw new NotSupportedException("This format is not supported");
            return Transformers[format].TransformToFile(data, properties);
        }


        public Dictionary<string, object> TransformFromFile(string format, string mapping, Stream file)
        {
            if (!Transformers.ContainsKey(format))
                throw new NotSupportedException("This format is not supported");
            return Transformers[format].TransformFromFile(mapping, file);
        }

        public Dictionary<string, object> TransformFromFile(string format, Mapping.Mapping mapping, Stream file)
        {
            if (!Transformers.ContainsKey(format))
                throw new NotSupportedException("This format is not supported");
            return Transformers[format].TransformFromFile(mapping, file);
        }

        public IList<T> TransformFromFile<T>(string format, Mapping.Mapping mapping, Stream file)
        {
            if (!Transformers.ContainsKey(format))
                throw new NotSupportedException("This format is not supported");
            return Transformers[format].TransformFromFile<T>(mapping, file);
        }

        public Mapping.Mapping GenerateMapping(string format, FileInfo file, IDictionary<string, object> parameters = null)
        {
            if(!MappingProviders.ContainsKey(format))
                throw new NotSupportedException("This format is not supported");
            return MappingProviders[format].GenerateMapping(file, parameters);
        }

        public Mapping.Mapping GenerateMapping(string format, Stream file, IDictionary<string, object> parameters = null)
        {
            if (!MappingProviders.ContainsKey(format))
                throw new NotSupportedException("This format is not supported");            
            
            return MappingProviders[format].GenerateMapping(file, parameters);
        }
    }
}
