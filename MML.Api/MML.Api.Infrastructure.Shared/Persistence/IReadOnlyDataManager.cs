using System;
using System.Collections.Generic;
using MML.Enterprise.Common.Mapping;

namespace MML.Enterprise.Persistence
{
    public interface IReadOnlyDataManager
    {
        T Find<T>(Criteria criteria);
        T Find<T>(Guid id, Criteria criteria = null);
        IList<T> FindAll<T>(Criteria criteria = null);
        DynamicResultSet DynamicFind<T>(Criteria criteria);
        DynamicResultSet DynamicFindAll<T>(Criteria criteria = null);
        int GetCount<T>(Criteria criteria = null);
        T GetTotals<T>(Criteria criteria = null);
        IList<T> GetFilterTypeResultSet<T>(Criteria criteria = null);
        Dictionary<string, List<KeyValuePair<string, string>>> GetFilterTypes<T>(IList<T> results, Criteria criteria);
        IList<IDynamicColumnValue> GetDynamicColumnHeaders<T>(Criteria criteria = null);
        IDictionary<string, object> DynamicTotal<T>(Criteria criteria);
    }
}
