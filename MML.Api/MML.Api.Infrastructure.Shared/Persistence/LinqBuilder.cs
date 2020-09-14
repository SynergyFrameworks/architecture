using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.ComponentModel;
using Microsoft.WindowsAzure.Storage.Table;

using NPOI.SS.Formula.Functions;
using MML.Enterprise.Persistence.Azure;
using MML.Enterprise.Persistence.Azure.Extensions;

namespace MML.Enterprise.Persistence
{
    //inspired largely by http://equivalence.co.uk/archives/819
    public class LinqBuilder : ILinqBuilder
    {
        public Expression<Func<DynamicTableEntity, bool>> GenerateLambda<T>(
            IList<TableQueryParameters> parametersList) where T : class
        {
            if (parametersList == null || parametersList.Count == 0)
                throw new Exception("Can not create linq statement without parameters.");

            ParameterExpression parameterExpression = Expression.Parameter(typeof(DynamicPersistentEntity), "entity");

            //rather than break if condition or parameter name are not set, ignore that parameter and move on.
            //to start we need to initialize the expression with the first parameters item, then add to it with the rest.
            int i = 0;
            bool validParametersFound = false;
            while (i < parametersList.Count && !validParametersFound)
            {
                if (CheckForNull(parametersList[i]))
                    validParametersFound = true;
                else
                    i++;
            }
            if (validParametersFound)
            {
                Expression fullExpression = GenerateExpression(parameterExpression, parametersList[i]);

                for (i = 1; i < parametersList.Count; i++)
                {
                    if (CheckForNull(parametersList[i]))
                        fullExpression = Expression.And(fullExpression,
                            GenerateExpression(parameterExpression, parametersList[i]));
                }
                return Expression.Lambda<Func<DynamicTableEntity, bool>>(fullExpression, parameterExpression);
            }
            throw new Exception("No valid parameters passed to create linq statement.");
        }

        private Expression GenerateExpression(ParameterExpression exParameter, TableQueryParameters parameters)
        {
            Expression exProperty = Expression.Property(exParameter, parameters.PropertyName);
            Expression exValue = Expression.Constant(parameters.Value);

            var info = (PropertyInfo)((MemberExpression)exProperty).Member;

            if (info.PropertyType == typeof(decimal) || info.PropertyType == typeof(decimal?))
                throw new Exception("Can not query against decimal values, ATS does not support decimals.");

            switch (parameters.Comparator)
            {
                case TableQueryParameters.Comparators.EqualTo:
                    {
                        return Expression.Equal(exProperty, exValue);
                    }
                case TableQueryParameters.Comparators.GreaterThan:
                    {
                        return Expression.GreaterThan(exProperty, exValue);
                    }
                case TableQueryParameters.Comparators.LessThan:
                    {
                        return Expression.LessThan(exProperty, exValue);
                    }
                case TableQueryParameters.Comparators.GreaterThanOrEqualTo:
                    {
                        return Expression.GreaterThanOrEqual(exProperty, exValue);
                    }
                case TableQueryParameters.Comparators.LessThanOrEqualTo:
                    {
                        return Expression.LessThanOrEqual(exProperty, exValue);
                    }
            }
            return null;
        }

        private bool CheckForNull(TableQueryParameters parameters)
        {
            return parameters.Comparator != new TableQueryParameters.Comparators() && parameters.PropertyName != null;
        }
    }
}
