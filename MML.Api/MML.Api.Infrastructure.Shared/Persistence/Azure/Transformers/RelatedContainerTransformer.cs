using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace MML.Enterprise.Persistence.Azure.Transformers
{
    public class RelatedContainerTransformer : IAzureTransformer
    {
     //   public AzureEntityManager EntityManager { get; set; } 
        public DynamicPersistentEntity TransformToAzureObject(PersistentEntity obj)
        {
            dynamic entity = new DynamicPersistentEntity();
            var properties = obj.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttributes<AzureCollectionAttribute>().Any());

            foreach (var property in properties)
            {
                var list = property.GetValue(obj) as IList;
                if (list == null) continue;
                var attribute = property.GetCustomAttributes<AzureCollectionAttribute>().FirstOrDefault();
            }
            return entity;
        }

        public T TransformFromAzureObject<T>(DynamicPersistentEntity obj)
        {
            throw new NotImplementedException();
        }
       
    }
}
