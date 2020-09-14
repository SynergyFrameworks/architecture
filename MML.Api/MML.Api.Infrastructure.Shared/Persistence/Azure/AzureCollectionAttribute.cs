namespace MML.Enterprise.Persistence.Azure
{
    public  class AzureCollectionAttribute : System.Attribute
    {
        public readonly string TransformedPropertyName;

        public AzureCollectionAttribute(string transformedPropertyName)
        {
            this.TransformedPropertyName = transformedPropertyName;
        }
    }
}
