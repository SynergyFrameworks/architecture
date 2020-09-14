using System.Collections.Generic;

namespace MML.Enterprise.Persistence
{
    /// <summary>
    /// The IQuery Interface is used to identify custom Query classes which define how to extract data through Dapper.
    /// The custom query objects should at minimum supply a string query to recieve the most basic dataset of their type
    /// and a criteria object populated with any required parameters.  Each Query should have its own unique type specified
    /// as the typeparam T. This data type is used as the unique lookup key to find the appropriate query when attempting to execute.
    /// </summary>
    /// <typeparam name="T">
    /// POCO with Getter Setter properties matching the column names in the result set of the Query.
    /// The result of query executions will almost always be a single instance or list of this data type.
    /// </typeparam>
    public interface IQuery<T>
    {
        QueryInfo GetQuery(Criteria criteria);
        QueryInfo GetCountQuery(Criteria criteria);
        QueryInfo GetTotalsQuery(Criteria criteria);
        QueryInfo GetFilterTypeResultSetQuery(Criteria criteria = null);
        Dictionary<string, List<KeyValuePair<string, string>>> GetFilterTypes(IList<T> results, Criteria criteria);
        Criteria CopyParametersWithoutColumnFilters(Criteria criteria);
        QueryInfo GetDynamicColumnsListQuery(Criteria criteria = null);
    }
}
