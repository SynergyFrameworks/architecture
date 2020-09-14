using MML.Enterprise.Persistence.Azure;
using System.Collections.Generic;

namespace MML.Enterprise.Persistence
{
    public interface IDapperEntityManager : IEntityManager
    {
        void SetMapping(IDictionary<string, string> obj);

        IList<T> FindAll<T>(IList<TableQueryParameters> parameters = null, int startPage = 1, int pageSize = 20) where T : class;
        T FindSp<T>(string spName, IList<TableQueryParameters> parameters = null);
        IList<T> FindAllSp<T>(string spName, IList<TableQueryParameters> parameters = null); 


    }
}
