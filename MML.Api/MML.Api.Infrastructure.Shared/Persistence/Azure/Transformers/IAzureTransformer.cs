namespace MML.Enterprise.Persistence.Azure.Transformers
{
    public interface IAzureTransformer
    {
        DynamicPersistentEntity TransformToAzureObject(PersistentEntity obj);
        T TransformFromAzureObject<T>(DynamicPersistentEntity obj);
    }
}
