using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace MML.Enterprise.Common.DataFile
{
    public abstract class AbstractDataFileTransformer : IDataFileTransformer
    {
        public virtual Stream TransformToFile(Mapping.Mapping mapping, IList<Dictionary<string, object>> data)
        {
            throw new NotImplementedException();
        }

        public virtual Stream TransformToFile(Mapping.Mapping mapping, IList<Dictionary<string, object>> data, IDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public virtual Stream TransformToFile(IList data, IList<string> properties = null)
        {
            throw new NotImplementedException();
        }

        public virtual Dictionary<string, object> TransformFromFile(string mapping, Stream file)
        {
            throw new NotImplementedException();
        }

        public virtual Dictionary<string, object> TransformFromFile(Mapping.Mapping mapping, Stream file)
        {
            throw new NotImplementedException();
        }

        public virtual IList<T> TransformFromFile<T>(Mapping.Mapping mapping, Stream file)
        {
            throw new NotImplementedException();
        }
    }
}
