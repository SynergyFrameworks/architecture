using Dapper;

namespace MML.Enterprise.Persistence
{
    public class QueryInfo
    {
        public string Query { get; set; }
        public DynamicParameters Parameters { get; set; }
        public int? CommandTimeout { get; set; }
    }
}
