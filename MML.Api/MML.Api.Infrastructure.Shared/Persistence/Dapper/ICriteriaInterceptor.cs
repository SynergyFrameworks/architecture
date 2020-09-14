namespace MML.Enterprise.Persistence.Dapper
{
    public interface ICriteriaInterceptor
    {
        void OnQuery<T>(object sender, Criteria criteria);
    }
}
