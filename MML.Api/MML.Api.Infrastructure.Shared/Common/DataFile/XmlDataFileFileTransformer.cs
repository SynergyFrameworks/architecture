using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using MML.Enterprise.Common.Mapping;
using MML.Enterprise.Common.Extensions;

namespace MML.Enterprise.Common.DataFile
{
    public class XmlDataFileFileTransformer : AbstractDataFileTransformer
    {
        public override Stream TransformToFile(Mapping.Mapping mapping, IList<Dictionary<string, object>> data)
        {
            var output = new MemoryStream();
            new XDocument(new XElement(mapping.ParentName,data.Select(d=> WriteObject(d,mapping.ObjectName,mapping.PropertyMappings)))).Save(output);
            return output;
        }

        public override Stream TransformToFile(Mapping.Mapping mapping, IList<Dictionary<string, object>> data, IDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public Stream TransformToFile(Mapping.Mapping mapping, object data)
        {
            var output = new MemoryStream();
            new XDocument(new XElement(mapping.ParentName, mapping.PropertyMappings)).Save(output);
            return output;
        }

        private XElement WriteObject(Dictionary<string, object> item, string objectName, IList<PropertyMapping> propertyMappings)
        {
            
            return new XElement(objectName,
                                       item.Select(
                                           i =>
                                           WriteProperty(
                                               propertyMappings.FirstOrDefault(pm => pm.PropertyName == i.Key),
                                               item.Values)));              
        }

        private XElement WriteObject(object item,string objectName, IList<PropertyMapping> mappings)
        {
            var attributes = mappings.Where(m => m.MappingInfo.Contains("@"));
            var elements = mappings.Where(m => !m.MappingInfo.Contains("@"));
            var element = new XElement(objectName,
                                       elements.Select(
                                           e => WriteObjectProperty(e, item.GetPropertyValue(e.PropertyName))));


            return AddAttributes(element, item, attributes.ToList());
        }

        private XElement WriteObjectProperty(PropertyMapping mapping, object item)
        {
            if (mapping.PropertyMappings.Any())
            {
                return WriteObject(item, mapping.MappingInfo, mapping.PropertyMappings);
            }
            return new XElement(mapping.MappingInfo, item.GetPropertyValue(item.ToString()));
        }

        private XElement AddAttributes(XElement element, object item, IList<PropertyMapping> attributes)
        {
            foreach (var attribute in attributes)
            {
                element.SetAttributeValue(attribute.MappingInfo.Replace("@", ""), item.GetPropertyValue(attribute.PropertyName));
            }
            return element;
        }

        private XElement WriteProperty(PropertyMapping propertyMapping,object value)
        {
            if (propertyMapping == null) return null;
            var item = value as Dictionary<string, object>;
            if (item != null)
            {
                return WriteObject(item, propertyMapping.PropertyName,
                                   propertyMapping.PropertyMappings);
            }
            var list = value as IList<string>;
            return list != null ? WriteList(propertyMapping.MappingInfo, list) : new XElement(propertyMapping.PropertyName, value.ToString());
        }

        

        private XElement WriteList(string elementName, IList<string> value)
        {
            return new XElement(elementName +"s", value.Select(v=> new XElement(elementName,v) ));
        }

    }
}
    