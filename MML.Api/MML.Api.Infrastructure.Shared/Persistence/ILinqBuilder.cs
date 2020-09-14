using System;
using System.Collections.Generic;
using System.Linq.Expressions;
//using NPOI.SS.Formula.Functions;
using Microsoft.WindowsAzure.Storage.Table;
using MML.Enterprise.Common.Persistence;
using MML.Enterprise.Persistence.Azure;

namespace MML.Enterprise.Persistence
{
    public interface ILinqBuilder
    {
        Expression<Func<DynamicTableEntity, bool>> GenerateLambda<T>(IList<TableQueryParameters> parametersList) where T : class;
    }
}
