namespace MML.Enterprise.Persistence.Azure
{
    public interface IAzureInterceptor
    {
        void OnTableInteraction(object sender, PersistentEntity entity);
    }
}
