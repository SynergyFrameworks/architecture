using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MML.Enterprise.Common.Mapping
{
    public class XmlMappingProvider : IMappingProvider
    {
        //assume mapping source is a directory
        public string MappingSource { get; set; }
        public IList<Mapping> GenerateMappings()
        {
            var dir = new DirectoryInfo(MappingSource);
            return dir.GetFiles("*.XmlMapping").Select(f=> GenerateMapping(f)).ToList();
        }
        public Mapping GenerateMapping(FileInfo file, IDictionary<string,object> parameters = null)
        {
            var doc = XDocument.Load(file.FullName);
            var mapping = new Mapping {ObjectName = doc.Root.Attribute("name").Value};
            mapping.PropertyMappings = GeneratePropertyMappings(doc.Root.Elements("property"));            

            return mapping;
        }

        public Mapping GenerateMapping(Stream file, IDictionary<string, object> parameters = null)
        {
            throw new NotImplementedException();
        }

        public Mapping GenerateMapping(FileInfo file, IList<HeaderMetadata> metadata, IDictionary<string, object> parameters = null)
        {
            throw new NotImplementedException();
        }

        private IList<PropertyMapping> GeneratePropertyMappings(IEnumerable<XElement> propertyMappings)
        {
            if (propertyMappings == null || !propertyMappings.Any())
                return null;
            return propertyMappings.Select(pm => new PropertyMapping
                {
                    MappingInfo = pm.Attribute("mappingInfo").Value,
                    PropertyName = pm.Attribute("name").Value,
                    PropertyMappings = GeneratePropertyMappings(pm.Elements("property"))
                }).ToList();
        }
    }
}
