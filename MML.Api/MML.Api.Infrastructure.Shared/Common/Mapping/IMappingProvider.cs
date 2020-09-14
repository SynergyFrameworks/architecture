using System.Collections.Generic;
using System.IO;

namespace MML.Enterprise.Common.Mapping
{
    public interface IMappingProvider
    {
        string MappingSource { get; set; }
        IList<Mapping> GenerateMappings();
        Mapping GenerateMapping(FileInfo file, IDictionary<string,object> parameters = null);
        Mapping GenerateMapping(Stream file, IDictionary<string, object> parameters = null);
        Mapping GenerateMapping(FileInfo file, IList<HeaderMetadata> metadata, IDictionary<string, object> parameters = null);
    }
}
