using Dapper;
using log4net;
using MML.Enterprise.Common.DataStructures;
using MML.Enterprise.Common.Extensions;
using MML.Enterprise.Common.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MML.Enterprise.Persistence
{
    /// <summary>
    /// Query objects have 4 primary goals:
    /// 1. Return a single instance or set of type T based on given parameters.
    /// 2. Return a total count of results based on given parameters without paging.
    /// 3. Return an instance of type T with total values across all results without paging.
    /// 4. Return a Dictionary with string keys equal to the names of columns that can be filtered,
    ///    and List of string values which contain the possible filter options for those columns.
    /// 
    /// The BaseQuery object facilitates these goals by providing a variety of helper methods,
    /// as well as a generic implementation of goals 2-4 given a custom implementation of goal 1
    /// that meets certain requirements.  These requirements are as follows by goal number:
    /// 
    /// All Goals:
    ///     a. Columns must be aliased with ' AS ' + Column Name.
    ///     b. If the query string does not start with SELECT (for example if there is a variable declaration at the start)
    ///        Then MainQueryStartIndicator must be injected into the query string before the main SELECT.
    ///        
    /// Goal 3 requirement for generic implementation:
    ///     Columns to be totaled must be of type decimal, int or float, or nullable of those types.
    ///     
    /// Goal 4 requirement for generic implementation:
    ///     Columns to be filtered on must be tagged in T with the ResultSetFilterAttribute.
    /// </summary>
    /// <typeparam name="T">Result Type used to catch the query results.</typeparam>
    public abstract class BaseQuery<T> : IQuery<T>
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(BaseQuery<T>));
        protected string[] RequiredParameters;
        protected string[] SearchSkipStrings = new[] { "Logo", "Picture" };
        protected string[] InternalSortColumns;
        protected bool dynamicQuery = false;
        protected int MaxFilterResults = 20;
        protected string AliasModifier = "Filter";
        protected string searchValueParameter = "searchValue";

        #region string constants
        protected const string NoValueFilter = "No Value";
        protected const string MainQueryStartIndicator = "--MAIN QUERY START";
        protected const string DotReplacementForParameters = "_DOT_REPLACEMENT_TO_NOT_BREAK_PARAM_";
        protected const string StuffStart = "STUFF((";
        protected const string ForXmlPath = "FOR XML PATH";
        protected const string WhereAndGroupBy = @"
        WHERE
            {0}
        GROUP BY
            {1}";
        protected HashSet<char> StuffNoise = new HashSet<char>( new[] { '{', '}', '"' });
        protected HashSet<char> StuffColumnStartConflicts = new HashSet<char>(new[] { '\'', ' ', '+' });
        protected const string NesetedObjectJoinSelectClause = @"{0}""{1}"":""', {2}.{3}, '""";
        protected HashSet<string> EqualityChecks = new HashSet<string>(new[] { "=", " LIKE ", " IN ", "<>", ">", "<" });
        protected const string AllInclusiveFilterKey = "All";
        protected const string AllInclusiveFilterValue = "All";

        private const string NoBracketPadding = " {0} ";
        private const string PaddingWithBrackets = " [{0}] ";
        private const string CountStart = @"
                COUNT(1) AS Count
			FROM (
			SELECT
				1 AS One";

        private const string CountEnd = @"
            ) AS countQuery";

        private const string ColumnFilter = @"
                AND ('All' IN @{0}
                      OR {1} IN @{0})";
        
        //Most common SQL aggregate functions with and without a space before parenthesis.
        private List<string> AggregateFunctions = new List<string> { "COUNT(",  "COUNT (",
                                                                     "SUM(",    "SUM (",
                                                                     "AVG(",    "AVG (",
                                                                     "MIN(",    "MIN (",
                                                                     "MAX(",    "MAX (" };
        private Dictionary<SortOrder, string> AggregateBySortOrder = new Dictionary<SortOrder, string> { { SortOrder.ASC, "MIN({0})" }, { SortOrder.DESC, "MAX({0})" }};
        #endregion string constants

        #region Paging
        /// <summary>
        /// Add Order By and Offset Fetch for paging if required by the Query and supported by the Criteria.
        /// </summary>
        /// <param name="criteria">If StartPage is set then PageSize must also be set.</param>
        /// <returns></returns>
        public string GenerateExtraClause(Criteria criteria)
        {
            var extraClause = string.Empty;
            if (!CheckCriteriaForPagingAndSorting(criteria))
                return extraClause;
            
            extraClause = criteria.SortOrders.Collection.Aggregate(@"
            ORDER BY ", (current, sortOrder) => current + AddBracketsIfAlias(sortOrder.Key) + sortOrder.Value + ",");
            extraClause = extraClause.Remove(extraClause.Length - 1);
            if (criteria.StartPage != null && criteria.PageSize != null)
            {
                extraClause = extraClause + @"
            OFFSET " + criteria.StartPage * criteria.PageSize + " ROWS FETCH NEXT " + criteria.PageSize +
                              " ROWS ONLY";
            }
            return extraClause;
        }

        /// <summary>
        /// Add Order By and Offset Fetch for paging if required by the Query and supported by the Criteria.
        /// </summary>
        /// <param name="criteria">If StartPage is set then PageSize must also be set.</param>
        /// <param name="query">query to be paged and sorted.</param>
        /// <returns>query with order by and paging added.  If ordered by a column in a nested object then the query is modified to accomodate.</returns>
        public string GenerateExtraClause(Criteria criteria, string query)
        {
            var extraClause = string.Empty;

            if (!CheckCriteriaForPagingAndSorting(criteria))
                return extraClause;

            var sortableColumns = dynamicQuery ? GetSelectColumns(query).Keys.ToList() : GetSortableColumnsByType(typeof(T));
            var nestedSortableColumns = GetSortableNestedColumns();
            if (nestedSortableColumns != null && nestedSortableColumns.Any())
            {
                foreach (var nestedObj in nestedSortableColumns)
                {
                    sortableColumns.AddRange(nestedObj.Value.Select(v => (nestedObj.Key.Key + "." + v)));
                }
            }
            var validSortColumns = new SequentialDictionary<string, SortOrder>();
            var requiredNestedSortColumns = new List<string>();
            foreach(var sortOrder in criteria.SortOrders.Collection)
            {
                if (sortableColumns.Any(s => CheckColumnName(sortOrder.Key, s)))
                    validSortColumns.Add(sortOrder.Key, sortOrder.Value);
                var split = sortOrder.Key.Split('.');
                if (split.Count() < 1 || nestedSortableColumns.Keys.All(k => k.Value != split[0]))
                    continue;

                validSortColumns.Add(sortOrder.Key, sortOrder.Value);
                requiredNestedSortColumns.Add(split[0]);
            }

            if (!validSortColumns.Any())
                return extraClause;
            
            extraClause = validSortColumns.Collection.Aggregate(@"
            ORDER BY ", (current, sortOrder) => current + AddBracketsIfAlias(sortOrder.Key) + sortOrder.Value + ",");
            extraClause = extraClause.Remove(extraClause.Length - 1);

            if (criteria.StartPage != null && criteria.PageSize != null)
            {
                extraClause = extraClause + @"
            OFFSET " + criteria.StartPage * criteria.PageSize + " ROWS FETCH NEXT " + criteria.PageSize +
                              " ROWS ONLY";
            }
            return extraClause;
        }

        protected virtual List<string> GetSortableColumnsByType(Type type)
        {
            //return type.GetProperties().Where(p => p.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(p.PropertyType)).Select(n => n.Name).ToList();
            var cols = type.GetProperties().Select(t => t.Name).ToList();
            if(InternalSortColumns != null)
                cols.AddRange(InternalSortColumns);
            return cols;
        }

        /// <summary>
        /// Useful in order by, this will add brackets around a column alias but not an indexed value.
        /// i.e. 'a.Key' will not get brackets because this will cause SQL to interpret it as the literal value 'a.key'
        /// rather than the column Key on the dataset a.  'Key' on the other hand would get brackets, and this is important
        /// because Key is a keyword and will cause the whole query to fail if it does not get brackets.
        /// </summary>
        /// <param name="selectValue">string value of a column name or column alias</param>
        /// <returns>selectValue if it contains a '.', otherwise selectValue between [].  Either way it has a space added before and after.</returns>
        protected virtual string AddBracketsIfAlias(string selectValue)
        {
            var properPadding = selectValue.Contains('.') ? NoBracketPadding : PaddingWithBrackets;
            return string.Format(properPadding, selectValue);
        }

        /// <summary>
        /// If StartPage is set then PageSize must also be set.
        /// If PageSize is set but StartPage is not, or is 0, then set StartPage to 1.
        /// Ensure that any provided SortColumns are valid properties of the result object.
        /// If query is paged and no SortColumns are provided, Set the first property of the result object as the Sort Column.
        /// </summary>
        /// <param name="criteria"></param>
        protected virtual bool CheckCriteriaForPagingAndSorting(Criteria criteria)
        {
            //if no paging or sorting, return false to not generate extra clause.
            if (criteria.StartPage == null && criteria.PageSize == null && (criteria.SortOrders == null || !criteria.SortOrders.Any()))
                return false;

            if (criteria.StartPage != null && criteria.PageSize == null)
                throw new SqlPagingException("If Start Page is provided then the query is expected to be paged and a page size must also be provided.");

            //if page size is set but no start page, start at the beginning.
            if (criteria.PageSize != null && criteria.StartPage == null)
                criteria.StartPage = 0;

            //At this point we either have sort orders or paging, so make sure there is at least one valid sort column and there are no invalid ones.
            var availableColumns = new HashSet<string>(typeof(T).GetProperties().Select(m => m.Name.ToLower())).ToList();
            var nestedSortableColumns = GetSortableNestedColumns();
            if(nestedSortableColumns != null && nestedSortableColumns.Any())
            {
                foreach(var nestedObj in nestedSortableColumns)
                {
                    availableColumns.AddRange(nestedObj.Value.Select(v => (nestedObj.Key.Key + "." + v).ToLower()));
                }
            }
            if(InternalSortColumns != null)
                availableColumns.AddRange(InternalSortColumns.Select(c => c.ToLower()));


            if (criteria.SortOrders == null)
                criteria.SortOrders = new SequentialDictionary<string, SortOrder>();

            if (!criteria.SortOrders.Any())
                criteria.SortOrders.Add(GetDefaultSort(availableColumns), SortOrder.ASC);
            else
            {
                if (dynamicQuery)
                    if (criteria.DynamicHeaders == null)
                        Log.Warn("Dynamic query is being sorted without Dynamic Columns set in Criteria, sorting on one of the dynamic columns will throw an exception.");
                    else
                        availableColumns.AddRange(criteria.DynamicHeaders.Select(dh => dh.ToLower()));
                
                foreach (var col in criteria.SortOrders.Keys)
                {
                    if (!availableColumns.Contains(col.ToLower()))
                        throw new NonExistantColumnException(col, typeof(T).Name);
                }
            }
            return true;
        }

        protected virtual string GetDefaultSort(List<string> availableColumns)
        {
            return availableColumns.FirstOrDefault();
        }
        #endregion Paging

        #region Query Portion Helpers
        /// <summary>
        /// Helper method to add a search clause.  This requires SearchParameters to be set with Keys equal to select columns and values equal to search criteria.
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        protected string GenerateSearchClause(Criteria criteria)
        {
            var searchClause = string.Empty;
            if (criteria.SearchParameters == null || criteria.SearchParameters.Count <= 0)
                return searchClause;

            return AggregateSearchClause(criteria);
        }

        /// <summary>
        /// Given a search value, generate an AND clause for comparing that value to all appropriate columns in the query.
        /// Default implementation checks all string, String, DateTime and DateTime? type columns.  Override CheckColumnTypeForSearch
        /// to modify this behavior.
        /// </summary>
        /// <param name="criteria">SearchQuery should be set.  This value will be compared against all searchable fields.</param>
        /// <param name="query">This query can be partial as long as it contains the primary SELECT FROM clause.</param>
        /// <returns>Newline + AND (col LIKE '%search%' OR ...) for all searchable columns based on property type.</returns>
        protected virtual string GenerateSearchClause(Criteria criteria, string query)
        {
            var search = criteria.SearchQuery;
            if (string.IsNullOrEmpty(search) || string.IsNullOrEmpty(query))
                return string.Empty;
            var selectCols = GetSelectColumns(query);
            var availableColumns = new HashSet<string>(typeof(T).GetProperties()
                .Where(p => CheckColumnTypeForSearch(p)).Select(m => m.Name.ToLower()));
            //TODO: Revisit to add searching on subquery result.

            var searchCols = selectCols.Where(s => availableColumns.Any(k => CheckColumnName(k, s.Key.ToLower()) && !AggregateFunctions.Any(a => s.Value.ToUpper().Contains(a))));
            var searchParams = searchCols.Select(s => new KeyValuePair<string, object>(GetUnaliasedSelect(s.Value), search));
            foreach (var param in searchParams)
                if(!criteria.SearchParameters.ContainsKey(param.Key)) //this code will execute for the main query as well as total, count and filter queries.
                    criteria.SearchParameters.Add(param.Key, param.Value);
            return AggregateSearchClause(criteria);
        }

        /// <summary>
        /// Create an AND clause to compare column select values with string search clauses.  Each column can check against a unique string if desired.
        /// </summary>
        /// <param name="criteria">
        /// The SearchParams should contain the search info. Key should be the table index as it shows in the main SELECT.  Value is the string to search against.
        /// The search strings will be added to the Parameters collection as named parameters searchValue + index (e.g. searchValue0).
        /// </param>
        /// <returns>Newline + AND (col LIKE '%search%' OR ...) for all searchable columns based on property type.</returns>
        private string AggregateSearchClause(Criteria criteria)
        {
            var searchParameters = criteria.SearchParameters.ToList();
            if (!searchParameters.Any())
                return string.Empty;

            var searchClause = @"
                AND (";
            
            for(var i = 0; i < searchParameters.Count; i++)
            {
                searchClause += searchParameters[i].Key + " LIKE @" + searchValueParameter + " OR ";
            }
            
            searchClause = searchClause.Remove(searchClause.Length - 3);
            searchClause += ")";
            return searchClause;
        }

        protected void EnsureSearchParameter(Criteria criteria)
        {
            if (criteria.Parameters != null && criteria.Parameters.ContainsKey(searchValueParameter))
                return;

            var searchString = string.IsNullOrEmpty(criteria.SearchQuery) ? criteria.SearchParameters?.Any() == true ? criteria.SearchParameters.First().Value.ToString() : criteria.SearchQuery : criteria.SearchQuery;
            if (!string.IsNullOrEmpty(searchString))
            {
                if (criteria.Parameters == null)
                    criteria.Parameters = new SortedDictionary<string, object>();
                // DbString tells Dapper to consider the value a VARCHAR rather than NVARCHAR (which is the default).
                // Searching with an NVARCHAR parameter against VARCHAR columns causes a significant performance hit.
                criteria.Parameters.Add(searchValueParameter, new DbString() { Value = "%" + searchString + "%", IsAnsi = true, Length = searchString.Length + 2 });
            }
        }

        /// <summary>
        /// Create search string as used in performance business code
        /// </summary>
        /// <param name="parameters">All parameters for this query</param>
        /// <param name="ParametersToKeep">Parameters that are not for search and which we will keep</param>
        /// <returns></returns>
        protected string AddParams(SortedDictionary<string, object> parameters, IList<string> parametersToKeep = null)
        {
            if (parameters == null || !parameters.Any())
            {
                return string.Empty;
            }
            var searchParams = (parametersToKeep != null && parametersToKeep.Any()) ? parameters.Where(param => !parametersToKeep.Contains(param.Key)) : parameters;
            var filter = string.Empty;
            if (searchParams.Any())
            {
                filter = " AND (";
                filter = searchParams.Aggregate(filter, (current, param) => current + param.Key + " LIKE '%" + param.Value + "%'" + " OR ");
                filter = filter.Remove(filter.Length - 3) + ") ";

                var searchKeys = new List<string>(searchParams.Select(pair => pair.Key));
                foreach (var key in searchKeys)
                {
                    parameters.Remove(key); //Remove the search params as their work is done
                }
            }
            return filter;
        }
        #endregion Query Portion Helpers

        #region Query Parsing Helpers
        /// <summary>
        /// Given the Full Type name of a property, this method checks if it is a number type that can be totalled.
        /// This is used by the default implementation of GetTotalsQuery and is available for use in custom implementations.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        protected virtual bool CheckColumnTypeForTotals(string typeName)
        {
            return typeName == typeof(decimal).FullName
                || typeName == typeof(decimal?).FullName
                || typeName == typeof(int).FullName
                || typeName == typeof(int?).FullName
                || typeName == typeof(float).FullName
                || typeName == typeof(float?).FullName;
        }

        /// <summary>
        /// this is meant to be overriden in a custom query. It would be 
        /// </summary>
        /// <returns></returns>
        protected virtual List<KeyValuePair<string,string>> GetDynamicTotalsColumns(string query)
        {
            var result = new List<KeyValuePair<string, string>>();
            var pivotIndex = query.LastIndexOf("PIVOT", StringComparison.OrdinalIgnoreCase);
            if (pivotIndex < 0)
                return result;
            var queryEnd = query.Substring(pivotIndex);
            var pivotEndIndex = queryEnd.IndexOf(" AS ", StringComparison.OrdinalIgnoreCase);
            var parenStart = queryEnd.IndexOf('(') + 1;
            var pivot = pivotEndIndex > 0 ? queryEnd.Substring(parenStart, pivotEndIndex - parenStart).Trim() : queryEnd.Substring(parenStart).Trim();

            var aggregateType = AggregateFunctions.FirstOrDefault(f => pivot.StartsWith(f, StringComparison.OrdinalIgnoreCase));
            if (aggregateType == null)
                return result;

            if(aggregateType.StartsWith("SUM") || aggregateType.StartsWith("AVG"))
                aggregateType = @"
                    " + aggregateType + "COALESCE({0},0)) AS {0}";
            else
                aggregateType = @"
                    " + aggregateType + "{0}) AS {0}";

            pivot = pivot.Substring(pivot.IndexOf(" IN ", StringComparison.OrdinalIgnoreCase));
            pivot = pivot.Substring(pivot.IndexOf('('));
            //pivot statement will end with two parens, one for PIVOT() and one for IN ().
            //we cannot look for the first closing paren because column names can have parens in them.
            //since we can not be sure there are no spaces or newlines between the final two parens, do two lookups.
            //first substring removes open paren, second starts at index 0.
            pivot = pivot.Substring(1, pivot.LastIndexOf(')') - 1);
            pivot = pivot.Substring(0, pivot.LastIndexOf(')') - 1);
            var columns = pivot.Split(']');

            foreach(var column in columns)
            {
                if (string.IsNullOrEmpty(column))
                    continue;
                var format = column.Substring(column.IndexOf('[')).Trim() + "]";
                result.Add(new KeyValuePair<string, string>(format, string.Format(aggregateType, format)));
            }
            return result;
        }

        /// <summary>
        /// Given the Full Type name of a property, this method checks if it is a text or date type that can be searched on.
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        protected virtual bool CheckColumnTypeForSearch(PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetCustomAttributes(false).Any(a => a is NestedQueryObjectAttribute || a is DynamicColumnsAttribute))
                return false;

            if (SearchSkipStrings.Any(s => propertyInfo.Name.Contains(s)))
                return false;

            var typeName = propertyInfo.PropertyType.FullName;
            return typeName == typeof(string).FullName
                || typeName == typeof(String).FullName
                || typeName == typeof(DateTime).FullName
                || typeName == typeof(DateTime?).FullName;
        }

        /// <summary>
        /// Assumes that columns are aliased with ' AS '
        /// </summary>
        /// <param name="query"></param>
        /// <returns>
        /// List of KeyValuePairs where the key is the column alias
        /// and the value is the portion of the select for the specified alias including the AS (alias) but not an ending comma.
        /// </returns>
        protected virtual Dictionary<string, string> GetSelectColumns(string query)
        {
            if (query == string.Empty || !query.Contains(' '))
                return null;
            var offset = GetSelectOffset(query);
            var fromIndex = FindClosingFromIndex(offset, query);

            //this skips the SELECT at the beginning
            var selectBlock = query.Substring(offset, fromIndex - offset);
            var selectColumns = new Dictionary<string, string>();
            var index = 0;
            var startIndex = 0;
            while (index + 1 < selectBlock.Length)
            {
                startIndex = index + 1;
                var asIndex = SafeGetAsIndex(selectBlock, index);
                if (asIndex == -1)
                    break;
                var commaIndex = selectBlock.IndexOf(",", asIndex);
                var bracketIndex = selectBlock.IndexOf("[", asIndex);
                index = bracketIndex > 0 && bracketIndex < commaIndex ? selectBlock.IndexOf("]",asIndex) + 1 : commaIndex > 1 ? commaIndex : selectBlock.Length - 1;
                var key = selectBlock.Substring(asIndex + 4, index - asIndex - 4).Trim();
                var value = selectBlock.Substring(startIndex, index - startIndex).Trim();
                selectColumns.Add(key, value);
            }
            return selectColumns;
        }

        /// <summary>
        /// Find the as index for a column alias in a select.  This needs to properly account for the possibility of join queries with aliases.
        /// </summary>
        /// <param name="selectBlock"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected int SafeGetAsIndex(string selectBlock, int index)
        {
            var foundAcceptableAs = false;
            var asIndex = index;
            while (!foundAcceptableAs)
            {
                asIndex = selectBlock.IndexOf(" AS ", asIndex + 1, StringComparison.OrdinalIgnoreCase);
                var subString = asIndex == -1 ? string.Empty : selectBlock.Substring(index, asIndex - index);
                if (subString.Count(c => c == '(') == subString.Count(c => c == ')') || asIndex == -1)
                    foundAcceptableAs = true;
                
            }
            return asIndex;
        }

        /// <summary>
        /// Find the index at the end of the first SELECT or SELECT DISTINCT after the MainQueryStartIndicator if it is included.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        protected virtual int GetSelectOffset(string query)
        {
            if (query == string.Empty || !query.Contains(' '))
                return 0;
            var declareBlock = GetDeclareBlock(query);
            var mainQuery = query.Substring(declareBlock.Length);
            var hasDistinct = mainQuery.Trim().CollapseWhiteSpace().Split(' ')[1].StartsWith("DISTINCT", StringComparison.OrdinalIgnoreCase);
            return hasDistinct ?
                mainQuery.IndexOf("DISTINCT", StringComparison.OrdinalIgnoreCase) + 8 + declareBlock.Length
                : mainQuery.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase) + 6 + declareBlock.Length;
        }

        /// <summary>
        /// Find any declarations made at the start of a query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        protected virtual string GetDeclareBlock(string query)
        {
            return query.IndexOf(MainQueryStartIndicator) > 0 ? query.Substring(0, query.IndexOf(MainQueryStartIndicator) + MainQueryStartIndicator.Length) : string.Empty;
        }

        /// <summary>
        /// Check against column name with and without enclosing brackets.
        /// </summary>
        /// <param name="checkValue"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        protected virtual bool CheckColumnName(string checkValue, string columnName)
        {
            if (checkValue.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
            var checkValueWithBrackets = "[" + checkValue + "]";
            return checkValueWithBrackets.Equals(columnName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Find the instance of FROM that ends a SELECT while skipping over any subqueries within the SELECT.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        protected virtual int FindClosingFromIndex(int startIndex, string query)
        {
            var selectIndex = query.IndexOf("SELECT", startIndex, StringComparison.OrdinalIgnoreCase);
            var fromIndex = query.IndexOf("FROM", startIndex, StringComparison.OrdinalIgnoreCase);
            //add 4 to get end of the text "FROM"
            return fromIndex < selectIndex || selectIndex == -1 ? fromIndex : FindClosingFromIndex(fromIndex + 4, query);
        }

        protected virtual string FindAliasInJoin(string join)
        {
            if (string.IsNullOrEmpty(join))
                return join;

            var split = join.ToUpper().Split((char[])null,StringSplitOptions.RemoveEmptyEntries);

            var aliasIndex = 0;

            for(aliasIndex = 0; aliasIndex < split.Length; aliasIndex++)
            {
                if (split[aliasIndex] == "ON" || split[aliasIndex] == "WITH")
                    break;
            }

            return split[aliasIndex - 1];
        }

        protected virtual string GetFromBlock(string query, int fromIndex = 0)
        {
            var whereIndex = query.IndexOfExact("WHERE", fromIndex, true, true);
            return whereIndex > fromIndex ? query.Substring(fromIndex, whereIndex - fromIndex) : query.Substring(fromIndex);
        }

        /// <summary>
        /// Given a query from a SELECT clause that is linked to the main query through its WHERE clause, identify and return the linking AND clause.
        /// N.B. Column references in the WHERE clause should include the Alias even if it is not required by SQL.
        /// </summary>
        /// <param name="query">Subquery from a SELECT statement that may link to the main query through a comparison in the WHERE clause.</param>
        /// <param name="fromIndex">Optional parameter to specify the index of the main FROM clause. Can help to avoid issues with futher subqueries in the SELECT of this subquery.</param>
        /// <returns>
        /// KeyValuePair where the value is a list of AND clauses that do not link to the outside query,
        /// and the key is a list of KeyValuePairs where the key is the local alias and the value is the external query alias for comparisons that link the query to the main.
        /// Note that the actual comparator is lost in this implementation and is assumed to be '='.
        /// </returns>
        protected virtual KeyValuePair<List<KeyValuePair<string,string>>,List<string>> ExtractLinkAndClause(string query, int fromIndex = 0)
        {
            var whereIndex = query.IndexOfExact("WHERE", fromIndex, true, true);
            if (whereIndex == -1)
                return new KeyValuePair<List<KeyValuePair<string, string>>, List<string>>();

            //Find all of the aliases for tables in this query.
            var aliases = GetTableAliases(query, 0);

            var clauses = new List<string>();

            //extract the where clause and break it into comparisons.
            var whereClause = query.Substring(whereIndex + 5);
            var groupByIndex = whereClause.IndexOfExact("GROUP BY", true);

            if (groupByIndex > 0)
                whereClause = whereClause.Substring(0, groupByIndex);

            var splitByAnd = whereClause.ToUpper().Split(new[] { "AND " }, StringSplitOptions.None);

            var linkComparison = new List<KeyValuePair<string, string>>();

            foreach(var clause in splitByAnd)
            {
                //break the comparison into text elements
                var splitOnSpaces = clause.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                string outerAlias = null;
                string innerAlias = null;
                foreach(var entry in splitOnSpaces)
                {
                    //identify objects to be compared by whether or not they contain a dot.
                    var dot = entry.IndexOf('.');
                    if (dot < 0)
                        continue;

                    var alias = entry.Substring(0, dot).ToUpper();

                    //determine if this alias is from the current query or the main query.
                    if (aliases.Contains(alias))
                        innerAlias = entry;
                    else
                        outerAlias = entry;
                }
                //if this comparison contained an alias not found in the current subquery then add both sides of the comparison to the key return collection.
                if (outerAlias != null)
                    linkComparison.Add(new KeyValuePair<string, string>(innerAlias, outerAlias));
                //Otherwise add the clause to the value list of the return collection.
                else
                    clauses.Add(clause);
            }
            
            return new KeyValuePair<List<KeyValuePair<string, string>>, List<string>>(linkComparison, clauses);
        }

        protected virtual HashSet<string> GetTableAliases(string query)
        {
            var aliases = new HashSet<string>();
            if (query == null || query.Length == 0)
                return aliases;

            var joined = query.ToUpper().Split(new[] { "JOIN " }, StringSplitOptions.RemoveEmptyEntries);

            foreach(var join in joined)
            {
                aliases.Add(FindAliasInJoin(join));
            }

            return aliases;
        }

        protected virtual HashSet<string> GetTableAliases(string query, int selectIndex)
        {
            var fromIndex = FindClosingFromIndex(selectIndex + 6, query);

            if (fromIndex > 0)
                fromIndex += 4;
            else
                return new HashSet<string>();

            var fromBlock = GetFromBlock(query, fromIndex);

            return GetTableAliases(fromBlock);
        }

        /// <summary>
        /// Return the portion of a select before the alias.
        /// </summary>
        /// <param name="select"></param>
        /// <returns></returns>
        protected virtual string GetUnaliasedSelect(string select)
        {
            if (string.IsNullOrEmpty(select))
                return select;
            var asIndex = select.IndexOf(" AS ");
            if (asIndex < 1)
                return select;
            return select.Substring(0, asIndex);
        }

        protected virtual string CleanStuffSelect(string select)
        {
            while (select.Length > 0 && StuffColumnStartConflicts.Contains(select.First()))
                select = select.Substring(1);
            while (select.Length > 0 && StuffColumnStartConflicts.Contains(select.Last()))
                select = select.Substring(0, select.Length - 1);

            if (select.ExceptBlanks().StartsWith("convert(nvarchar(", StringComparison.OrdinalIgnoreCase))
            {
                select = select.Substring(select.IndexOf(',') + 1);
                select = select.Substring(0, select.Length - 1);
            }
            else if (select.ExceptBlanks().StartsWith("coalesce(convert(nvarchar(", StringComparison.OrdinalIgnoreCase))
            {
                select = select.Substring(select.IndexOf(',') + 1);
                select = select.Substring(0, select.IndexOf(',') - 1);
            }
            return select;
        }

        /// <summary>
        /// Apply a search comparison to a stuff query select field.  The search value is assumed to be stored in a parameter with the default name.
        /// </summary>
        /// <param name="select">The source of the select column (e.g. column name)</param>
        /// <returns></returns>
        protected virtual string CheckSelectForSearch(string select)
        {
            select = CleanStuffSelect(select).Trim();
            var startIndex = 0;
            if (select.StartsWith("COALESCE("))
                startIndex = 9;
            if (select.StartsWith("ISNULL("))
                startIndex = 7;

            // sometimes values in stuffs need to be converted to varchar or coalesced so as to not break the string output.  We need to parse that away if present.
            if (startIndex > 0)
            {
                var selects = select.Substring(startIndex, select.Length - startIndex - 1).Split(',');
                return selects.Aggregate("", (current, next) => string.IsNullOrEmpty(next) || SearchSkipStrings.Any(ss => next.EndsWith(ss, StringComparison.OrdinalIgnoreCase))
                                                                ? current
                                                                    : next.IndexOf("CONVERT(", StringComparison.OrdinalIgnoreCase) > 0
                                                                        ? current + next + ","
                                                                            : current + next + " LIKE @" + searchValueParameter + " OR ");
            }
            return string.IsNullOrEmpty(select) ? string.Empty : select + " LIKE @" + searchValueParameter + " OR ";
        }

        public virtual QueryInfo GetUnpagedUngroupedQuery(Criteria criteria)
        {
            EnsureNoPaging(criteria);
            if (criteria.Parameters == null)
                criteria.Parameters = new SortedDictionary<string, object>();
            EnsureColumnFilterCriteriaInitialized(criteria);

            var queryInfo = GetQuery(criteria);
            if (queryInfo == null)
                return null;
            queryInfo.Query = RemoveGroupClause(queryInfo);
            return queryInfo;
        }
        #endregion Query Parsing Helpers

        #region IQuery Support
        /// <summary>
        /// Implement this in custom query classes to define the base query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public abstract QueryInfo GetQuery(Criteria criteria);

        /// <summary>
        /// Default implementation strips any sorting/paging, replaces the select list with SELECT 1 as One, and wraps the whole thing in a Count(1).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public virtual QueryInfo GetCountQuery(Criteria criteria)
        {
            EnsureNoPaging(criteria);
            EnsureSearchParameter(criteria);
            var info = GetQuery(criteria);
            if (info == null)
                return null;
            info.Query = GetQueryWithColumnSubset(new List<string> { CountStart },info.Query) + CountEnd;
            
            return info;
        }

        /// <summary>
        /// Default implementation assumes Column names are aliased with ' AS '.
        /// Group By clause is removed from the query if present and all decimal, int and float values (nullable or not) are totalled.
        /// If the field already has an aggregate function it is retained, otherwise it is set to SUM.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public virtual QueryInfo GetTotalsQuery(Criteria criteria)
        {
            EnsureNoPaging(criteria);
            EnsureSearchParameter(criteria);
            var info = GetQuery(criteria);
            if (info == null)
                return null;
            var newQuery = RemoveGroupClause(info);
            var selectCols = GetSelectColumns(newQuery);
            var dynamicColumns = GetDynamicTotalsColumns(newQuery);
            var availableColumns = new HashSet<string>(typeof(T).GetProperties()
                .Where(p => CheckColumnTypeForTotals(p.PropertyType.FullName)).Select(m => m.Name.ToLower()));
            var aggregateCols = selectCols.Where(s => availableColumns.Any(k => CheckColumnName(k,s.Key.ToLower()))).ToList();
            
            var aggregateColsFinal = new List<KeyValuePair<string, string>>();
            foreach(var col in aggregateCols)
            {
                if (AggregateFunctions.Any(a => col.Value.ToUpper().Contains(a)))
                    aggregateColsFinal.Add(col);
                else
                    aggregateColsFinal.Add(new KeyValuePair<string, string>(col.Key, "SUM(" + col.Value.Substring(0, col.Value.IndexOf(" AS ", StringComparison.OrdinalIgnoreCase)) + ") AS " + col.Key));
            }
            aggregateColsFinal.AddRange(dynamicColumns);
            info.Query = GetQueryWithColumnSubset(aggregateColsFinal.Select(v => v.Value), newQuery);
            return info;
        }

        /// <summary>
        /// Default implementation returns null if no properties in the type T are flagged with the ResultSetFilterAttribute.
        /// The result of this query is meant to be fed into GetFilterTypes<typeparamref name="T"/>(IList<typeparamref name="T"/> results).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public virtual QueryInfo GetFilterTypeResultSetQuery(Criteria criteria = null)
        {
            EnsureSearchParameter(criteria);
            var filterColumns = typeof(T).GetProperties().Where(p => p.GetCustomAttributes(false).Any(a => a is ResultSetFilterAttribute)).Select(i => i.Name).ToList();
            var nestedQueryObjectColumns = GetFilterableNestedColumns();
            var hasTraditionalFilterColumns = filterColumns != null && filterColumns.Any();
            var hasNestedFilterColumns = nestedQueryObjectColumns != null && nestedQueryObjectColumns.Any();
            if (!hasNestedFilterColumns && !hasTraditionalFilterColumns)
                return null;
            
            var queryInfo = GetUnpagedUngroupedQuery(criteria);
            if (queryInfo == null)
                return null;
            var selectColumns = GetSelectColumns(queryInfo.Query);

            string joins = string.Empty;

            var filterSelectColumns = selectColumns.Where(s => filterColumns.Any(f => CheckColumnName(f,s.Key))).Select(c => c.Value).ToList();
            if (hasNestedFilterColumns)
            {
                filterSelectColumns.AddRange(GetOuterSelectsForStuffJoins(nestedQueryObjectColumns, AliasModifier));
                foreach(var nestedColumn in nestedQueryObjectColumns)
                {
                    var collectionFilterColumns = selectColumns.Where(s => nestedQueryObjectColumns.Any(f => CheckColumnName(f.Key.Value, s.Key))).ToDictionary(c => c.Key, c => c.Value);
                    var splitPieces = GetStuffQueriesByColumnAlias(collectionFilterColumns, AliasModifier);

                    joins = ConvertStuffsToJoins(splitPieces, nestedColumn.Value.Select(v => v.Key).ToList(), criteria.SearchQuery, true);
                }
            }

            if (!filterSelectColumns.Any())
                return null;
            return FinalizeStuffJoinedQuery(queryInfo, joins, filterSelectColumns, true);
        }

        /// <summary>
        /// Default implementation of GetFilterTypes expects at least one property in the result object to be tagged with the ResultSetFilter attribute.
        /// If MappingOverrides are set, they will be used in place of specified values, otherwise distinct values will be returned with matching Key and Value.
        /// N.B. This can only really be used directly when the result set is not paged, otherwise use IReadOnlyDataManager.GetFilterTypeResultSet<typeparamref name="T"/>(Critera criteria = null)
        /// to get the values to use here..
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results">This should be the result set of the query execution.</param>
        /// <returns>
        /// A Dictionary with:
        /// a key for each property in T that has a ResultSetFilterAttribute
        /// a list of values denoting all possible filter options for that field after search and filters have been applied.
        /// </returns>
        public virtual Dictionary<string, List<KeyValuePair<string, string>>> GetFilterTypes(IList<T> results, Criteria criteria)
        {
            EnsureSearchParameter(criteria);
            if (results == null || !results.Any())
                return null;
            var filters = new Dictionary<string, List<KeyValuePair<string, string>>>();
            var filterColumns = GetResultFilterColumns();
            var nestedFilterColumns = GetFilterableNestedColumns();
            foreach(var filterableObject in nestedFilterColumns)
            {
                foreach(var nestedFilter in filterableObject.Value)
                {
                    filterColumns.Add(filterableObject.Key.Key + "." + nestedFilter.Key, nestedFilter.Value);
                }
            }

            var parameters = criteria.Parameters;

            foreach(var filterColumn in filterColumns)
            {
                var filterAttribute = filterColumn.Value;
                if(filterAttribute != null)
                {
                    var propName = filterColumn.Key;
                    var unfilteredKeyVals = GetUnfilteredKey(propName);
                    filters.Add(propName, unfilteredKeyVals.Value.Select(s => new KeyValuePair<string, string> (unfilteredKeyVals.Key , s )).ToList());
                    var filterList = new HashSet<string>();

                    var overrides = filterAttribute.GetMappingOverrides();
                    var reducedFilters = ReduceColumnFilterList(results, criteria, propName.Replace(".", DotReplacementForParameters));
                    foreach(var property in reducedFilters)
                    {
                        var key = overrides.ContainsKey(property) ? overrides[property].Key : property == null ? NoValueFilter : property;

                        if (filterList.Contains(key))
                            continue;
                        else
                            filterList.Add(key);

                        if (overrides.ContainsKey(property))
                            filters[propName].Add(overrides[property]);
                        else
                            filters[propName].Add(new KeyValuePair<string, string>(property, property));
                    }
                    if (filters[propName].Count > MaxFilterResults)
                        filters.Remove(propName);
                    else
                        filters[propName] = filters[propName].OrderBy(f => f.Key == GetUnfilteredKey(propName).Key ? "_" : f.Key).ToList();
                }
            }

            return filters;
        }

        /// <summary>
        /// In order to properly filter the available Column Filters, we need to briefly maintain two sets of Parameters.
        /// This method facilitates that by creating a copy of the Parameters without the Column Filters.
        /// This parameter list can be fed to GetFilterTypeResultSetQuery which will then fill in the missing params with "All".
        /// The original list can then be passed to GetFilterTypes to ensure that the correct subset of Column Filters are returned.
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public Criteria CopyParametersWithoutColumnFilters(Criteria criteria)
        {
            EnsureSearchParameter(criteria);
            var newParameters = new SortedDictionary<string, object>();
            var filterColumns = GetResultFilterColumns();
            var nestedFilterColumns = GetFilterableNestedColumns();
            foreach (var filterableObject in nestedFilterColumns)
            {
                foreach (var nestedFilter in filterableObject.Value)
                {
                    filterColumns.Add(filterableObject.Key.Key + DotReplacementForParameters + nestedFilter.Key, nestedFilter.Value);
                }
            }
            foreach (var param in criteria.Parameters)
            {
                if (filterColumns.ContainsKey(param.Key))
                    newParameters.Add(param.Key,  GetUnfilteredKey(param.Key).Value);
                else
                    newParameters.Add(param.Key, param.Value);
            }
            
            return new Criteria
            {
                StartPage = criteria.StartPage,
                PageSize = criteria.PageSize,
                Parameters = newParameters,
                SortOrders = criteria.SortOrders,
                SearchParameters = criteria.SearchParameters,
                DynamicHeaders = criteria.DynamicHeaders,
                SearchQuery = criteria.SearchQuery
            };
        }

        /// <summary>
        /// Get a list of all column header values.
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public virtual QueryInfo GetDynamicColumnsListQuery(Criteria criteria = null)
        {
            EnsureSearchParameter(criteria);
            //Strip base query of grouping and paging info.
            var queryInfo = GetUnpagedUngroupedQuery(criteria);
            var query = queryInfo.Query;
            if (query == string.Empty || !query.Contains(' '))
                return null;

            var dynamicColumnsSelectColumns = GetDynamicColumnsSelectColumns(queryInfo.Query);
            
            var splitPieces = GetStuffQueriesByColumnAlias(dynamicColumnsSelectColumns);

            var joins = ConvertStuffsToJoins(splitPieces, new List<string> { "ColumnHeader" }, null, true);
            var dynamicColumnAliases = new List<string>();
            
            foreach(var stuffColumn in splitPieces)
            {
                //TODO: Clearly this will not work if we actually have multiple dynamic column groupings.  We can revisit.
                dynamicColumnAliases.Add(stuffColumn.Key + ".ColumnHeader AS ColumnHeader");
            }

            return FinalizeStuffJoinedQuery(queryInfo, joins, dynamicColumnAliases, true);
        }
        #endregion IQuery Support

        #region Query Modifiers
        /// <summary>
        /// Make sure that queries will not be paged by removing Sort Orders, Start Page and Page Size.
        /// This is called by Totals, Filter and Count queries.  
        /// </summary>
        /// <param name="criteria"></param>
        protected virtual void EnsureNoPaging(Criteria criteria)
        {
            if (criteria == null)
                return;

            criteria.SortOrders = new SequentialDictionary<string, SortOrder>();
            criteria.StartPage = null;
            criteria.PageSize = null;
        }

        /// <summary>
        /// Remove the last GROUP BY clause that is not within a subquery.  This allows for swapping out select clause and adding DISTINCT.
        /// </summary>
        /// <param name="queryInfo"></param>
        /// <returns></returns>
        protected virtual string RemoveGroupClause(QueryInfo queryInfo)
        {
            var groupIndex = queryInfo.Query.LastIndexOf("GROUP BY", StringComparison.OrdinalIgnoreCase);

            //If the last GROUP BY is in a subquery, ignore it.
            if (groupIndex > 0 && queryInfo.Query.Substring(groupIndex).IndexOf(" AS ", StringComparison.OrdinalIgnoreCase) > 0)
                groupIndex = -1;

            return groupIndex > 0 ? queryInfo.Query.Substring(0, groupIndex) : queryInfo.Query;
        }

        /// <summary>
        /// Remove the last GROUP BY clause that is not within a subquery.  This allows for swapping out select clause and adding DISTINCT.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        protected virtual string RemoveGroupClause(string query)
        {
            var groupIndex = FindIndexOfClosingStatement(query, "GROUP BY");
            var orderByIndex = FindIndexOfClosingStatement(query, "ORDER BY");
            return groupIndex > 0 ? query.Substring(0, groupIndex) : orderByIndex > 0 ? query.Substring(0,orderByIndex) : query;
        }

        /// <summary>
        /// Find the index of the last GROUP BY not part of a subquery.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        protected virtual int FindIndexOfClosingStatement(string query, string statement)
        {
            var groupIndex = query.LastIndexOf("GROUP BY", StringComparison.OrdinalIgnoreCase);

            //If the last GROUP BY is in a subquery, ignore it.
            if (groupIndex > 0 && query.Substring(groupIndex).IndexOf(" AS ", StringComparison.OrdinalIgnoreCase) > 0)
                groupIndex = -1;

            return groupIndex;
        }

        /// <summary>
        /// Find the index of the last GROUP BY not part of a subquery.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        protected virtual int FindIndexOfClosingWhere(string query)
        {
            var whereIndex = query.IndexOfExact("WHERE", true, true);

            //If the last GROUP BY is in a subquery, ignore it.
            if (whereIndex > 0 && query.Substring(whereIndex).IndexOf(" AS ", StringComparison.OrdinalIgnoreCase) > 0)
                whereIndex = -1;

            return whereIndex;
        }

        /// <summary>
        /// Strip the column list out of the outermost SELECT clause of a query and replace it with a custom set of columns.
        /// </summary>
        /// <param name="columnList"></param>
        /// <param name="query"></param>
        /// <param name="forceDistinct">Allows caller to force SELECT DISTINCT.</param>
        /// <returns></returns>
        protected virtual string GetQueryWithColumnSubset(IEnumerable<string> columnList, string query, bool forceDistinct = false)
        {
            var queryStart = query.Substring(0, GetSelectOffset(query));
            if (forceDistinct && !queryStart.EndsWith("DISTINCT"))
                queryStart += " DISTINCT";
            var fromIndex = FindClosingFromIndex(queryStart.Length, query);

            if(columnList != null && columnList.Any())
            {
                queryStart = columnList.Aggregate(queryStart, (current, nextCol) => current + Environment.NewLine + nextCol + ",");
                queryStart = queryStart.Remove(queryStart.Length - 1);
            }
            else
            {
                queryStart = queryStart + Environment.NewLine + "NULL AS NoTotals";
            }

            return queryStart + Environment.NewLine + query.Substring(fromIndex);
        }

        /// <summary>
        /// Add additional AND clauses to an existing query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="additionalAnds"></param>
        /// <returns></returns>
        protected virtual string AddToWhereClause(string query, string additionalAnds)
        {
            var finalGroupByIndex = FindIndexOfClosingStatement(query, "GROUP BY");
            var finalWhereIndex = FindIndexOfClosingWhere(query);

            if (finalWhereIndex == -1)
            {
                finalWhereIndex = finalGroupByIndex < 0 ? query.Length - 1 : finalGroupByIndex;
                query = query.Insert(finalWhereIndex,@"
                WHERE
                    1=1
                    " + additionalAnds + Environment.NewLine);
            }
            else
            {
                if (finalGroupByIndex == -1)
                    finalGroupByIndex = query.Length;
                query = query.Insert(finalGroupByIndex, additionalAnds + Environment.NewLine);
            }
            return query;
        }

        /// <summary>
        /// Given a query, add filtering, searching and sorting to it.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="criteria"></param>
        /// <param name="generateExtraClause"></param>
        /// <returns></returns>
        protected virtual string FilterAndSearchQuery(string query, Criteria criteria, bool generateExtraClause = true)
        {
            //TODO: Alter Stuff in main SELECT query.
            EnsureSearchParameter(criteria);
            var subQuerySorts = criteria.SortOrders.Collection.Where(so => so.Key.Contains('.')).GroupBy(i => i.Key.Split('.')[0]).ToDictionary(d => d.Key, d => d.ToList());
            var subQueryFilters = GetSubQueryFilters(criteria);
            var subqueryJoin = subQueryFilters.Any() ? "INNER JOIN" : subQuerySorts.Any() || !string.IsNullOrEmpty(criteria.SearchQuery) ? "LEFT OUTER JOIN" : null;

            string joins = string.Empty;

            var selectColumns = GetSelectColumns(query);
            var nestedQueryObjectColumns = GetFilterableNestedColumns();
            var joinedQueryAliases = new List<string>();
            if (subqueryJoin != null)
            {
                var collectionFilterColumns = new Dictionary<string, string>();
                var selectsToUpdate = new Dictionary<string, string>();
                foreach(var column in selectColumns)
                {
                    //yeah nested looping but bear in mind usually not more than on nested query object col.
                    var nestedObjectColumn = nestedQueryObjectColumns.FirstOrDefault(f => CheckColumnName(f.Key.Value, column.Key));
                    if (nestedObjectColumn.Key.Key != null)
                    {
                        collectionFilterColumns.Add(nestedObjectColumn.Key.Key, column.Value);
                        selectsToUpdate.Add(nestedObjectColumn.Key.Value, column.Value);
                    }
                }
                //get the query out of the stuff for collection cols.
                var stuffQueriesByAlias = GetStuffQueriesByColumnAlias(collectionFilterColumns);
                var stuffQueriesToUpdateByAlias = GetStuffQueriesByColumnAlias(selectsToUpdate);
                joinedQueryAliases = stuffQueriesByAlias.Select(s => s.Key).ToList();
                var nestedSorts = new Dictionary<string, SortOrder>();
                if (criteria.SortOrders != null && criteria.SortOrders.Any())
                {
                    nestedSorts = criteria.SortOrders.Collection.Where(s => s.Key.IndexOf('.') > 0).ToDictionary(d => d.Key, d => d.Value);
                }

                Dictionary<string, KeyValuePair<string, SortOrder>> nestedSortsByAlias = null;
                if (nestedSorts.Any())
                {
                    //N.B. this could break if we have multiple stuff objects with sorts on properties of the same name.
                    var strippedSortOrders = nestedSorts.ToDictionary(d => d.Key.Substring(d.Key.IndexOf('.') + 1), d => d.Value);
                    nestedSortsByAlias = nestedSorts.ToDictionary(d => d.Key.Substring(0, d.Key.IndexOf('.')), d => new KeyValuePair<string, SortOrder>(d.Key.Substring(d.Key.IndexOf('.') + 1), d.Value));
                    joins = ConvertStuffsToJoins(stuffQueriesByAlias, strippedSortOrders, criteria.SearchQuery, true, subqueryJoin, subQueryFilters);
                }
                else
                    joins = ConvertStuffsToJoins(stuffQueriesByAlias, new List<string>(), criteria.SearchQuery, false, subqueryJoin, subQueryFilters);

                if(nestedSorts.Any() || subQueryFilters.Any())
                {
                    var updatedStuffSelects = ReStuffQueriesByColumnAlias(UpdateStuffSelectQueries(stuffQueriesToUpdateByAlias, nestedSortsByAlias, subQueryFilters));
                    foreach (var updatedSelect in updatedStuffSelects)
                        selectColumns[updatedSelect.Key] = updatedSelect.Value;
                }
                query = GetQueryWithColumnSubset(selectColumns.Values, query);
            }

            //TODO: add in to this a check on the bit col from stuff joins.
            var searchClause = GenerateSearchClause(criteria, query);
            if (joinedQueryAliases.Any() && !string.IsNullOrEmpty(criteria.SearchQuery))
            {
                var joinedQuerySearches = joinedQueryAliases.Aggregate("", (current, next) => current + " OR " + next + ".Search = 1");
                searchClause = string.IsNullOrEmpty(searchClause)
                    ? "AND (" + joinedQuerySearches.Substring(joinedQuerySearches.IndexOf(" OR ") + 4) + ")"
                    : searchClause.Substring(0, searchClause.Length - 1) + joinedQuerySearches + ")";
            }
            var columnFilters = GetColumnFilters(query, criteria);

            var finalGroupByIndex = FindIndexOfClosingStatement(query, "GROUP BY");

            //If there is a GROUP BY clause and sort orders based on stuffed collections, we need to add to the GROUP BY to support the sort.
            if (finalGroupByIndex > 0 && criteria.SortOrders.Collection.Any(k => k.Key.Contains('.')))
            {
                var sortGroupBys = criteria.SortOrders.Collection.Select(kvp => kvp.Key).Where(k => k.Contains('.')).Aggregate(" ", (current, sortOrder) => current + sortOrder + ",");
                query = query.Insert(finalGroupByIndex + 8, sortGroupBys);
            }

            query = AddToWhereClause(query, searchClause + columnFilters);
            var finalWhereIndex = FindIndexOfClosingWhere(query);

            query = query.Insert(finalWhereIndex, joins + Environment.NewLine);

            if (generateExtraClause)
                query += GenerateExtraClause(criteria, query);

            EnsureDefaultParameters(criteria);
            EnsureColumnFilterCriteriaInitialized(criteria);

            return query;
        }
        #endregion Query Modifiers

        #region Column Filters
        /// <summary>
        /// Get ResultSetFilterAttributes by Propery.
        /// </summary>
        /// <returns>Dictionary with key = Property Name, Value = the ResultSetFilterAttribute for that Propery.</returns>
        protected virtual Dictionary<string, ResultSetFilterAttribute> GetResultFilterColumns(Type type = null)
        {
            if (type == null)
                type = typeof(T);
            return type.GetProperties().Where(pi => pi.GetCustomAttributes(false).Any(a => a is ResultSetFilterAttribute)).ToDictionary(prop => prop.Name,
                prop => (ResultSetFilterAttribute)prop.GetCustomAttributes(false).FirstOrDefault(a => a is ResultSetFilterAttribute));
        }

        /// <summary>
        /// Get key for Unfiltered list.  Defaults to All, All.
        /// </summary>
        /// <returns></returns>
        protected virtual KeyValuePair<string, string[]> GetUnfilteredKey(string propertyName)
        {
            var val = GetAllInclusiveFilterValue(propertyName);
            return new KeyValuePair<string, string[]>("All", val);
        }

        /// <summary>
        /// This returns a series of ANDs for a WHERE clause, one for each property on the result object flagged with a ResultSetFilterAttribute.
        /// It is intended to be inserted into the query through either a string.Format or concatenation within the child query object.
        /// </summary>
        /// <param name="query">The query parameter must contain the primary SELECT...FROM statement which maps to the result type.</param>
        /// <returns>
        /// This returns a string which contains a series of ANDs for a WHERE clause, one for each property on the result object flagged with a ResultSetFilterAttribute.
        /// </returns>
        protected virtual string GetColumnFilters(string query, Criteria criteria)
        {
            var filter = string.Empty;
            var filterColumns = GetResultFilterColumns();
            var selectColumns = GetSelectColumns(query);

            if (criteria.Parameters == null)
                criteria.Parameters = new SortedDictionary<string, object>();

            foreach (var col in filterColumns)
            {
                if (criteria.Parameters.ContainsKey(col.Key) && ((string[])criteria.Parameters[col.Key]).All(s => s != AllInclusiveFilterValue))
                {
                    var selectColumn = selectColumns.FirstOrDefault(kvp => CheckColumnName(col.Key, kvp.Key));
                    if(!string.IsNullOrEmpty(selectColumn.Key))
                    {
                        filter += string.Format(ColumnFilter, col.Key, GetUnaliasedSelect(selectColumn.Value));
                    }
                }
            }
            
            return filter;
        }

        /// <summary>
        /// This returns a series of ANDs for a WHERE clause, one for each property on the result object flagged with a ResultSetFilterAttribute.
        /// It is intended to be inserted into the query through either a string.Format or concatenation within the child query object.
        /// </summary>
        /// <param name="query">The query parameter must contain the primary SELECT...FROM statement which maps to the result type.</param>
        /// <returns>
        /// This returns a string which contains a series of ANDs for a WHERE clause, one for each property on the result object flagged with a ResultSetFilterAttribute.
        /// </returns>
        protected virtual string GetSubQueryColumnFilters(string query, Dictionary<string,object> subQueryFilters)
        {
            var filter = string.Empty;
            var selectColumns = GetSelectKeysFromJsonStuffSelect(query);
            var selectsByAlias = selectColumns.Where(s => s.Length > 1).ToDictionary(s => s[0], s => s[1]);
            if (!selectsByAlias.Any())
                return filter;

            var filtersByAlias = subQueryFilters.ToDictionary(q => q.Key.Substring(q.Key.IndexOf(DotReplacementForParameters) + DotReplacementForParameters.Length), q => q);

            foreach (var parameter in filtersByAlias)
            {
                var key = parameter.Key;
                if (!selectsByAlias.ContainsKey(key))
                    continue;
                var selectColumn = CleanStuffSelect(selectsByAlias[key]);
                filter += string.Format(ColumnFilter, parameter.Value.Key, selectColumn);
            }

            return filter;
        }

        /// <summary>
        /// Helper method that takes a list of objects and a list of property names on those objects.
        /// The result is a distint list of property values by property name.
        /// This method is currently not in use but is provided for use in custom queries.
        /// </summary>
        /// <param name="results"></param>
        /// <param name="filterColumns"></param>
        /// <returns></returns>
        protected virtual Dictionary<string, List<string>> GetFullDistinctColumnFilterCombinations(IList<T> results, List<string> filterColumns)
        {
            var combinations = new Dictionary<string, List<string>>();
            
            foreach(var column in filterColumns)
            {
                combinations.Add(column, results.Select(r => r.GetPropertyValue(column).ToString()).ToList());
            }

            return combinations;
        }

        /// <summary>
        /// Find the list of available values for a filter based on other applied filters.
        /// This method generates unique keys for each of the filter result objects and then the list of available unique keys based on other filters,
        /// and then retuns the distinct list of values for the column in question that match between those two lists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="criteria"></param>
        /// <param name="currentColumn"></param>
        /// <returns></returns>
        protected virtual IList<string> ReduceColumnFilterList(IList<T> results, Criteria criteria, string currentColumn)
        {
            var filterColumns = GetResultFilterColumns().Keys.OrderBy(k => k).ToList();
            var nestedFilterColumns = GetFilterableNestedColumns();
            foreach (var filterableObject in nestedFilterColumns)
            {
                foreach (var nestedFilter in filterableObject.Value)
                {
                    var key = filterableObject.Key.Key + DotReplacementForParameters + nestedFilter.Key;
                    if(criteria.Parameters.ContainsKey(key))
                        filterColumns.Add(key);
                }
            }
            filterColumns = filterColumns.Where(f => ((string[])criteria.Parameters[f]).All(s => GetAllInclusiveFilterValue(f).All(i => i != s))).OrderBy(k => k).ToList();
            var uniqueValues = results.Select(r => GenerateUniqueKey(r, currentColumn, filterColumns)).GroupBy(g => g.Key).ToDictionary(d => d.Key, d => d.Select(i => i.Value));
            var availableUniqueKeys = GetAvailableUniqueKeys(criteria, currentColumn, filterColumns);
            var reducedList = availableUniqueKeys.Intersect(uniqueValues.Keys).SelectMany(k => uniqueValues[k]).Distinct().Where(i => i != null);
            return reducedList.ToList();
        }

        /// <summary>
        /// Generate a unique list of values by Column name to check against possible result values from the filter query.
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="currentColumn">The column for which you are currently constructing a filter list.</param>
        /// <param name="filterColumns">The list of columns with filters applied in the current query execution.</param>
        /// <returns>
        /// A list of strings where each entry is in the form of ColumnName1:ColumnValue1|ColumnName2:ColumnValue2| and so on.
        /// This list should represent all of the unique combinations outside of "All" passed in through the criteria.
        /// </returns>
        protected virtual List<string> GetAvailableUniqueKeys(Criteria criteria, string currentColumn, IEnumerable<string> filterColumns)
        {
            IEnumerable<string> keys = new[] { "" };
            var columnFilters = filterColumns.Where(c => c != currentColumn).ToDictionary(f => f, f => criteria.Parameters[f] as string[]);
            foreach(var column in columnFilters)
            {
                keys = from k in keys from c in column.Value.Where(v => GetAllInclusiveFilterValue(column.Key).All(i => i != v)) select k + column.Key + ":" + c + "|";
            }
            return keys.ToList();
        }

        /// <summary>
        /// return a key value pair where the key is the concatenation of all column names and values that are not the current column.
        /// </summary>
        /// <param name="result">Current T</param>
        /// <param name="currentColumn">Name of the column being reduced</param>
        /// <param name="filterColumns">sorted List of all filter columns</param>
        /// <returns></returns>
        protected virtual KeyValuePair<string,string> GenerateUniqueKey(T result, string currentColumn, IEnumerable<string> filterColumns)
        {
            var keyColumns = filterColumns.Where(c => c != currentColumn);
            return new KeyValuePair<string, string>(String.Join(string.Empty, keyColumns.Select(k => k + ":" + GetPropertyValue(result, k) + "|")), GetPropertyValue(result, currentColumn));
        }

        protected virtual string GetPropertyValue(T result, string propertyName)
        {
            if (propertyName.Contains(DotReplacementForParameters))
            {
                var dotNotation = propertyName.Split(new[] { DotReplacementForParameters }, StringSplitOptions.None);
                var collection = (IEnumerable<object>)result.GetPropertyValue(dotNotation[0]);
                if (collection == null)
                    return string.Empty;

                var container = collection.First();
                if (container == null)
                    return string.Empty;

                return container.GetPropertyValue(dotNotation[1]).SafeToString();
            }

            return result.GetPropertyValue(propertyName).SafeToString();
        }

        /// <summary>
        /// Replace criteria parameter names for subquery filters with the text DOT in place of the '.' and return list of those parameters and their values.
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        protected virtual Dictionary<string,object> GetSubQueryFilters(Criteria criteria)
        {
            var dotNotationParams = new Dictionary<string,object>();
            var keysToRemove = new List<string>();
            var valuesToAdd = new Dictionary<string, object>();
            foreach(var parameter in criteria.Parameters)
            {
                if (parameter.Key.Contains('.'))
                {
                    var newkey = parameter.Key.Replace(".", DotReplacementForParameters);
                    valuesToAdd.Add(newkey, parameter.Value);
                    keysToRemove.Add(parameter.Key);
                    dotNotationParams.Add(newkey, parameter.Value);
                }
                //Check for params that have already been cleaned so we don't have to loop twice.
                else if (parameter.Key.Contains(DotReplacementForParameters))
                {
                    dotNotationParams.Add(parameter.Key, parameter.Value);
                }
                
            }

            //need to break this out to avoid angering the enumerator.
            foreach (var key in keysToRemove)
                criteria.Parameters.Remove(key);
            foreach (var value in valuesToAdd)
                criteria.Parameters.Add(value.Key, value.Value);
            
            return dotNotationParams;
        }

        protected virtual string[] GetAllInclusiveFilterValue(string propertyName)
        {
            var property = typeof(T).GetProperty(propertyName);

            return property != null && property.PropertyType == typeof(bool) ? new[] { "0", "1" } : new[] { AllInclusiveFilterValue };
        }
        #endregion Column Filters

        #region Ensure Initialization
        /// <summary>
        /// Look at the properties on T which are flagged with ResultSetFilterAttribute and add entries into the Criteria Parameters for them if they do not already exist.
        /// Default to the value of GetUnfilteredKey().
        /// If a column has mapping overrides and passed in values, re-map them. 
        /// </summary>
        protected virtual void EnsureColumnFilterCriteriaInitialized(Criteria criteria)
        {
            var filterColumns = GetResultFilterColumns();
            if (criteria.Parameters == null)
                criteria.Parameters = new SortedDictionary<string, object>();
            foreach (var col in filterColumns)
                if (!criteria.Parameters.ContainsKey(col.Key))
                    criteria.Parameters.Add(col.Key,  GetUnfilteredKey(col.Key).Value);
                else
                {
                    var overrides = col.Value.GetReverseMappings();
                    if (overrides.Keys.Any())
                    {
                        var filterList = criteria.Parameters[col.Key] as string[];
                        if (filterList == null)
                            continue;

                        var remappedFilterList = new List<string>();
                        foreach (var filter in filterList)
                        {
                            if (overrides.ContainsKey(filter))
                                remappedFilterList.AddRange(overrides[filter]);
                            else
                                remappedFilterList.Add(filter);
                        }
                        criteria.Parameters[col.Key] = remappedFilterList.ToArray();
                    }
                }
        }

        /// <summary>
        /// Override RequiredParameters to contain a list of the string names of all required parameters that may
        /// not be passed in to your query.  This method will then ensure that they are added with null value to the
        /// parameter list.
        /// </summary>
        /// <param name="criteria"></param>
        protected virtual void EnsureDefaultParameters(Criteria criteria)
        {
            if (criteria == null || RequiredParameters == null)
                return;

            if (criteria.Parameters == null)
                criteria.Parameters = new SortedDictionary<string, object>();

            foreach (var param in RequiredParameters)
            {
                if (!criteria.Parameters.ContainsKey(param))
                    criteria.Parameters.Add(param, null);
            }
        }
        #endregion Ensure Initialization

        #region Nested Collections
        /// <summary>
        /// Get dynamic columns by alias.
        /// </summary>
        /// <param name="queryInfo"></param>
        /// <param name="selectColumns">Optional param if the calling method already parsed out selectColumns.</param>
        /// <returns></returns>
        public virtual Dictionary<string, string> GetDynamicColumnsSelectColumns(string query, Dictionary<string,string> selectColumns = null)
        {
            //Identify Json columns used to store Dynamic Column values.
            var dynamicColumnCollections = typeof(T).GetProperties().Where(pi => pi.GetCustomAttributes(false).Any(a => a is DynamicColumnsAttribute)).Select(prop => prop.Name);
            if (!dynamicColumnCollections.Any())
                return null;

            //Get the stuff queries used for any dynamic columns.
            selectColumns = selectColumns ?? GetSelectColumns(query);
            var dynamicColumnsSelectColumns = selectColumns.Where(s => dynamicColumnCollections.Any(f => CheckColumnName(f, s.Key))).ToDictionary(c => c.Key, c => c.Value);
            if (!dynamicColumnsSelectColumns.Any())
                return null;

            return dynamicColumnsSelectColumns;
        }

        protected virtual Dictionary<string,string> UpdateStuffSelectQueries(Dictionary<string,string> stuffSelectsByAlias, Dictionary<string, KeyValuePair<string,SortOrder>> nestedSortsByAlias, Dictionary<string,object> subQueryFilters)
        {
            var updatedStuffSelects = new Dictionary<string, string>();
            foreach(var stuffSelect in stuffSelectsByAlias)
            {
                //if(nestedSortsByAlias.ContainsKey(stuffSelect.Key))
                var filters = GetSubQueryColumnFilters(stuffSelect.Value, subQueryFilters);
                var updatedQuery = AddToWhereClause(stuffSelect.Value, filters);
                var selectKeys = GetSelectKeysFromJsonStuffSelect(stuffSelect.Value);
                if(nestedSortsByAlias != null)
                {
                    var orderBy = string.Empty;
                    foreach (var sort in nestedSortsByAlias)
                    {
                        var keys = selectKeys.FirstOrDefault(s => string.Equals(s[0], sort.Value.Key, StringComparison.OrdinalIgnoreCase));
                        if (keys != null)
                            orderBy = string.IsNullOrEmpty(orderBy) ? CleanStuffSelect(keys[1]) + " " + sort.Value.Value : orderBy + " " + sort.Value.Value + ", " + keys[1];
                    }
                    orderBy = @"
                ORDER BY
                    " + orderBy + Environment.NewLine;
                    var orderByIndex = stuffSelect.Value.IndexOfExact("ORDER BY", true);
                    updatedQuery = orderByIndex > 0 ? updatedQuery.Substring(0, orderByIndex) + orderBy : updatedQuery + orderBy;
                }
                updatedStuffSelects.Add(stuffSelect.Key, updatedQuery);
            }
            return updatedStuffSelects;
        }

        /// <summary>
        /// Get Sortable Nested Columns by Parent Column
        /// </summary>
        /// <returns>
        /// Dictionary of:
        /// Key - KeyValuePair:
        ///         key - Name of the custom getter property that returns deserialized objects.
        ///         value - Name of the column that stores the JSON Stuff.
        /// Value - List of sortable column names within the object.
        /// </returns>
        protected virtual Dictionary<KeyValuePair<string,string>,List<string>> GetSortableNestedColumns()
        {
            var nestedObjectColumns = typeof(T).GetProperties().Where(p => p.GetCustomAttributes(false).Any(a => a is NestedQueryObjectAttribute)).ToDictionary(p => p.Name, p => (NestedQueryObjectAttribute)p.GetCustomAttributes(false).FirstOrDefault(a => a is NestedQueryObjectAttribute)).ToList();

            return nestedObjectColumns.ToDictionary(n => new KeyValuePair<string,string>(n.Value.GetNameAlias(), n.Key), n => GetSortableColumnsByType(n.Value.GetNestedObjectType()));
        }

        /// <summary> 
        /// propertyName can be the collection of the nested object, or the json string
        /// </summary>
        protected Type GetNestedObjectType(string propertyName)
        {
            Type type = null;
            var property = typeof(T).GetProperties().FirstOrDefault(p => p.Name == propertyName);
            if (property != null)
            {               
                if (property.PropertyType.IsGenericType)
                {
                    type = property.PropertyType.GetGenericArguments().Single();
                }
                else
                {
                    var attr = (NestedQueryObjectAttribute)property.GetCustomAttributes(false).FirstOrDefault(a => a is NestedQueryObjectAttribute);
                    type = attr.GetNestedObjectType();
                }
            }
            return type;
        }

        /// <summary>
        /// Get Filterable Nested Columns by Parent Column
        /// </summary>
        /// <returns>
        /// Dictionary of:
        /// Key - KeyValuePair:
        ///         key - Name of the custom getter property that returns deserialized objects.
        ///         value - Name of the column that stores the JSON Stuff.
        /// Value - Dictionary:
        ///         key - Property name on the collection object that is filterable
        ///         value - the ResultSetFilterAttribute for the filterable column.
        /// </returns>
        protected virtual Dictionary<KeyValuePair<string, string>, Dictionary<string,ResultSetFilterAttribute>> GetFilterableNestedColumns()
        {
            var nestedObjectColumns = typeof(T).GetProperties().Where(p => p.GetCustomAttributes(false).Any(a => a is NestedQueryObjectAttribute)).ToDictionary(p => p.Name, p => (NestedQueryObjectAttribute)p.GetCustomAttributes(false).FirstOrDefault(a => a is NestedQueryObjectAttribute)).ToList();

            return nestedObjectColumns.ToDictionary(n => new KeyValuePair<string, string>(n.Value.GetNameAlias(), n.Key), n => GetResultFilterColumns(n.Value.GetNestedObjectType()));
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterableNestedColumns"></param>
        /// <returns></returns>
        protected virtual IEnumerable<string> GetOuterSelectsForStuffJoins(Dictionary<KeyValuePair<string, string>, Dictionary<string, ResultSetFilterAttribute>> filterableNestedColumns, string aliasModifier = null)
        {
            var selects = new List<string>();
            foreach(var nestedSelect in filterableNestedColumns)
            {
                var select = "CONCAT('[{";
                var comma = string.Empty;
                foreach(var column in nestedSelect.Value)
                {
                    select += string.Format(NesetedObjectJoinSelectClause, comma, column.Key, nestedSelect.Key.Value + aliasModifier, column.Key);
                    comma = ",";
                }
                select += "}]') AS " + nestedSelect.Key.Value;
                selects.Add(select);
            }
            return selects;
        }

        /// <summary>
        /// Take a Dictionary of SELECT Column values by Alias and return a Dictionary of Stuff queries by Alias.
        /// (Extract the query out of the Stuff For XML)
        /// </summary>
        /// <param name="stuffColumns">Key: Column alias. Value: Raw stuff query column.</param>
        /// <returns></returns>
        protected virtual Dictionary<string,string> GetStuffQueriesByColumnAlias(Dictionary<string,string> stuffColumns, string aliasModifier = null)
        {
            var splitPieces = new Dictionary<string, string>();

            foreach (var column in stuffColumns)
            {
                var stuffIndex = column.Value.IndexOf(StuffStart, StringComparison.OrdinalIgnoreCase);
                if (stuffIndex == -1)
                    continue;
                var index = column.Value.IndexOf(ForXmlPath, stuffIndex) > 1 ? column.Value.IndexOf(ForXmlPath, stuffIndex) : column.Value.Length - 1;
                var stuff = column.Value.Substring(stuffIndex + StuffStart.Length, index - stuffIndex - StuffStart.Length).Trim();
                Log.Debug("Found Stuff query:");
                Log.Debug(stuff);

                splitPieces.Add(column.Key + aliasModifier, stuff);
            }

            return splitPieces;
        }

        /// <summary>
        /// Take a Dictionary of SELECT Column values by Alias and return a Dictionary of Stuff queries by Alias.
        /// (Extract the query out of the Stuff For XML)
        /// </summary>
        /// <param name="stuffColumns">Key: Column alias. Value: Raw stuff query column.</param>
        /// <returns></returns>
        protected virtual Dictionary<string, string> ReStuffQueriesByColumnAlias(Dictionary<string, string> stuffColumns)
        {
            var reStuffed = new Dictionary<string, string>();
            foreach (var column in stuffColumns)
            {
                reStuffed.Add(column.Key, @"CONCAT('[',STUFF((
				    " + column.Value + @"
				    FOR XML PATH, TYPE).value('.[1]', 'varchar(max)')
				    , 1, 1, ''),']') AS " + column.Key);
            }

            return reStuffed;
        }

        /// <summary>
        /// Extract the Json concat string from the SELECT and strip out all of the string concat and Json related noise.
        /// </summary>
        /// <param name="stuffQuery"></param>
        /// <returns>A collection of string arrays which should contain alias and select value for the stuff json.</returns>
        protected virtual IEnumerable<string[]> GetSelectKeysFromJsonStuffSelect(string stuffQuery)
        {
            var offset = GetSelectOffset(stuffQuery);
            var fromIndex = stuffQuery.IndexOf("FROM", offset, StringComparison.OrdinalIgnoreCase);

            var selectBlock = stuffQuery.Substring(offset, fromIndex - offset);
            Log.Debug("Stuff Select Block: " + selectBlock);
            selectBlock = selectBlock.CollapseWhiteSpace().Trim();
            if (selectBlock.StartsWith("',"))
                selectBlock = selectBlock.Substring(2);
            if (selectBlock.EndsWith("'"))
                selectBlock = selectBlock.Substring(0,selectBlock.Length - 1);

            var selectRows = selectBlock.Split(new string[] { "\",\"" }, StringSplitOptions.RemoveEmptyEntries);
            return selectRows.Select(r => r.ExceptChars(StuffNoise).Split(':').Select(s => s.Trim()).ToArray());
        }

        //Loop through the json properties and convert them to SELECT columns.  Restrict to ColumnHeader for Dynamic Columns Header List.
        /// <summary>
        /// Loop through the collection of keys and values and convert into Value As Alias for select columns.
        /// </summary>
        /// <param name="keyValues">Collection of string arrays.  The string arrays are expected to be length 2 and contain the alias and value for a select.</param>
        /// <param name="restrictToColumns">List of aliases to restrict the result set to.  If null then the results will not be filtered.</param>
        /// <param name="prependAlias">Optional parameter to prepend text to this alias. Used for sorting and filtering.</param>
        /// <returns></returns>
        protected virtual List<string> GetStuffSelectColumns(IEnumerable<string[]> keyValues, List<string> restrictToColumns, string prependAlias = null)
        {
            var stuffSelectColumns = new List<string>();
            foreach (var keyValue in keyValues)
            {
                if (keyValue.Length != 2 || restrictToColumns != null && !restrictToColumns.Contains(keyValue[0]))
                    continue;
                stuffSelectColumns.Add(CleanStuffSelect(keyValue[1]) + " AS " + prependAlias + keyValue[0]);
                Log.DebugFormat("stuff column parsed to value: {0}, alias: {1}", keyValue[1], keyValue[0]);
            }
            return stuffSelectColumns;
        }

        //Loop through the json properties and convert them to SELECT columns.  Restrict to ColumnHeader for Dynamic Columns Header List.
        protected virtual List<string> GetStuffSelectColumns(string stuffQuery, List<string> restrictToColumns, string prependAlias = null)
        {
            var keyValues = GetSelectKeysFromJsonStuffSelect(stuffQuery);

            return GetStuffSelectColumns(keyValues, restrictToColumns, prependAlias);
        }

        //Loop through the json properties and convert them to SELECT columns.  Restrict to ColumnHeader for Dynamic Columns Header List.
        /// <summary>
        /// Loop through the collection of keys and values and convert into Value As Alias for select columns.
        /// </summary>
        /// <param name="keyValues">Collection of string arrays.  The string arrays are expected to be length 2 and contain the alias and value for a select.</param>
        /// <param name="restrictToColumns">List of aliases to restrict the result set to by SortOrder.  If null then the results will not be filtered.</param>
        /// <param name="prependAlias">Optional parameter to prepend text to this alias. Used for sorting and filtering.</param>
        /// <returns></returns>
        protected virtual List<string> GetStuffSelectColumns(IEnumerable<string[]> keyValues, Dictionary<string, SortOrder> restrictToColumns, string prependAlias = null)
        {
            var stuffSelectColumns = new List<string>();
            foreach (var keyValue in keyValues)
            {
                if (keyValue.Length != 2 || restrictToColumns != null && !restrictToColumns.ContainsKey(keyValue[0]))
                    continue;
                var selectValue = CleanStuffSelect(keyValue[1]);
                if(restrictToColumns != null && restrictToColumns.Keys.Any(k => string.Equals(k,keyValue[0], StringComparison.OrdinalIgnoreCase)))
                {
                    selectValue = string.Format(AggregateBySortOrder[restrictToColumns[keyValue[0]]], selectValue);
                }
                stuffSelectColumns.Add(selectValue + " AS " + prependAlias + keyValue[0]);
                Log.DebugFormat("stuff column parsed to value: {0}, alias: {1}", keyValue[1], keyValue[0]);
            }
            return stuffSelectColumns;
        }

        //Loop through the json properties and convert them to SELECT columns.  Restrict to ColumnHeader for Dynamic Columns Header List.
        protected virtual List<string> GetStuffSelectColumns(string stuffQuery, Dictionary<string, SortOrder> restrictToColumns, string prependAlias = null)
        {
            var keyValues = GetSelectKeysFromJsonStuffSelect(stuffQuery);

            return GetStuffSelectColumns(keyValues, restrictToColumns, prependAlias);
        }
        
        /// <summary>
        /// To facilitate searching and filtering we take a stuff query in the select clause and convert it into a subquery join on the same fields but with different output.
        /// If a search query is specified, we include a bit value of 0 or 1 based on whether or not any searchable fields were found.
        /// If a filter is applied to the stuff collection, that is applied in where clause of the join.
        /// </summary>
        /// <param name="stuff"></param>
        /// <param name="subQueryName"></param>
        /// <param name="joinType"></param>
        /// <param name="searchQuery"></param>
        /// <param name="stuffSelectColumns"></param>
        /// <param name="excludeGroupBy"></param>
        /// <param name="subQueryFilters"></param>
        /// <returns></returns>
        protected virtual string ConvertStuffToJoin(string stuff, string subQueryName, string joinType, string searchQuery, List<string> stuffSelectColumns, bool excludeGroupBy, Dictionary<string, object> subQueryFilters)
        {
            //Find the index of WHERE, GROUP BY and ORDER BY clauses in the query.
            var whereIndex = stuff.IndexOfExact("WHERE", true, true);
            var groupByIndex = stuff.IndexOfExact("GROUP BY", true, true);
            var orderByIndex = stuff.IndexOfExact("ORDER BY", true, true);
            var groupBy = string.Empty;

            // remove the existing group by clause. We will add a new one if necessary.
            stuff = RemoveGroupClause(stuff);
            
            var join = @"
                ) AS " + subQueryName + " ON ";
            var aliases = ExtractLinkAndClause(stuff);
            var joinIteration = "{0}.{1} = {2} AND ";

            //Loop through any aliases identified to link this subquery to the main query.
            for (var j = 0; j < aliases.Key.Count(); j++)
            {
                var alias = "OuterJoin" + j;
                //Add the inner alias to the select list to ensure we can join on it.
                stuffSelectColumns.Add(aliases.Key[j].Key + " AS " + alias);
                var dotIndex = aliases.Key[j].Key.IndexOf('.');
                //Add the join on this alias to the outside of the new subquery.
                join += string.Format(joinIteration, subQueryName, alias, aliases.Key[j].Value);
            }

            foreach(var select in stuffSelectColumns)
            {
                if (AggregateFunctions.Any(af => select.ToUpper().StartsWith(af)))
                    continue;
                var columnValue = select.Substring(0, select.IndexOf(" AS "));
                groupBy += string.IsNullOrEmpty(groupBy) ? @"
                GROUP BY
                    " + columnValue : ", " + columnValue;
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                var keys = GetSelectKeysFromJsonStuffSelect(stuff);

                string propertyName = subQueryName.Contains(AliasModifier) ? subQueryName.Replace(AliasModifier, string.Empty) : subQueryName;
                var nestedQueryObjectType = GetNestedObjectType(propertyName);
                if (nestedQueryObjectType != null)
                {
                    var availableColumns = new HashSet<string>(nestedQueryObjectType.GetProperties().Where(p => CheckColumnTypeForSearch(p)).Select(m => m.Name.ToLower()));
                    keys = keys.Where(s => availableColumns.Any(k => CheckColumnName(k, s.First().ToLower()))).ToList();
                }

                var searchSelect = "MAX(CASE WHEN ";
                searchSelect = keys.Aggregate(searchSelect, (current, search) => search.Length > 2 || AggregateFunctions.Any(f => CleanStuffSelect(search[1]).ToUpper().StartsWith(f)) ? current : current + CheckSelectForSearch(search[1]));
                searchSelect = searchSelect.Substring(0, searchSelect.Length - 3);
                searchSelect += "THEN 1 ELSE 0 END) AS Search";
                stuffSelectColumns.Add(searchSelect);
            }
            //Strip away the final trailing ' AND ' from the join.
            join = join.Substring(0, join.Length - 5);

            //Reconstruct the WHERE clause for this subquery without the linking AND.
            var updatedWhere = string.Empty;
            for (var k = 0; k < aliases.Value.Count(); k++)
            {
                var andClause = aliases.Value[k];
                if (k == 0)
                    updatedWhere = andClause;
                else
                    updatedWhere += @"
                        AND " + andClause;
            }

            if (string.IsNullOrEmpty(updatedWhere))
                updatedWhere = @" 1=1
                        ";

            if (subQueryFilters != null && subQueryFilters.Any())
            {
                var filters = GetSubQueryColumnFilters(stuff, subQueryFilters);
                updatedWhere += @"
                        " + filters + Environment.NewLine;
            }

            //Swap in the updated WHERE clause and add GROUP BY if necessary.
            var transformedStuffQuery = stuff.Substring(0, whereIndex) + @"
                WHERE
                    " + updatedWhere + groupBy;
            //Sub in the updated SELECT columns
            transformedStuffQuery = GetQueryWithColumnSubset(stuffSelectColumns, transformedStuffQuery);
            //Wrap the subquery in a JOIN
            transformedStuffQuery = @"
                " + joinType + @" (
                    " + transformedStuffQuery + join;
            Log.Debug("Transformed Stuff Query:");
            Log.Debug(transformedStuffQuery);
            return transformedStuffQuery;
        }

        //Process each stuff query (usually only one)
        protected virtual string ConvertStuffsToJoins(Dictionary<string,string> stuffQueriesByAlias, List<string> restrictToColumns, string searchQuery, bool excludeGroupBy = false, string joinType = "LEFT OUTER JOIN", Dictionary<string, object> subQueryFilters = null)
        {
            var joins = string.Empty;
            foreach (var stuffColumn in stuffQueriesByAlias)
            {
                var stuff = stuffColumn.Value;

                var stuffSelectColumns = GetStuffSelectColumns(stuff, restrictToColumns);


                var transformedStuffQuery = ConvertStuffToJoin(stuff, stuffColumn.Key, joinType, searchQuery, stuffSelectColumns, excludeGroupBy, subQueryFilters);
                
                joins += transformedStuffQuery;
            }
            return joins;
        }

        //Process each stuff query (usually only one)
        protected virtual string ConvertStuffsToJoins(Dictionary<string, string> stuffQueriesByAlias, Dictionary<string, SortOrder> restrictToColumns, string searchQuery, bool excludeGroupBy = false, string joinType = "LEFT OUTER JOIN", Dictionary<string,object> subQueryFilters = null)
        {
            var joins = string.Empty;
            foreach (var stuffColumn in stuffQueriesByAlias)
            {
                var stuff = stuffColumn.Value;

                var stuffSelectColumns = GetStuffSelectColumns(stuff, restrictToColumns);

                var transformedStuffQuery = ConvertStuffToJoin(stuff, stuffColumn.Key, joinType, searchQuery, stuffSelectColumns, excludeGroupBy, subQueryFilters);

                joins += transformedStuffQuery;
            }
            return joins;
        }

        protected virtual QueryInfo FinalizeStuffJoinedQuery(QueryInfo queryInfo, string joins, List<string> finalizedSelectColumns, bool forceDistinct = false)
        {
            //Sub in the updated SELECT columns
            queryInfo.Query = GetQueryWithColumnSubset(finalizedSelectColumns, queryInfo.Query, forceDistinct);

            var lastWHereIndex = queryInfo.Query.LastIndexOf("WHERE", StringComparison.OrdinalIgnoreCase);

            //If the last GROUP BY is in a subquery, ignore it
            if (lastWHereIndex > 0 && queryInfo.Query.Substring(lastWHereIndex).IndexOf(" AS ", StringComparison.OrdinalIgnoreCase) > 0)
                lastWHereIndex = -1;

            //Add in the stuff subqueries
            if (lastWHereIndex > 0)
                queryInfo.Query = queryInfo.Query.Substring(0, lastWHereIndex) + joins + Environment.NewLine + queryInfo.Query.Substring(lastWHereIndex);
            else
                queryInfo.Query = queryInfo.Query + joins;
            return queryInfo;
        }
        #endregion Nested Collections
    }
}
