namespace MML.Enterprise.Persistence.Dapper
{
    public interface IQueryInfoInterceptor
    {
        void OnQuery<T>(object sender, QueryInfo queryInfo);
    }
}
