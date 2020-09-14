using MML.Enterprise.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Persistence.Dapper
{
    public class QueryString
    {
        protected const string MainQueryStartIndicator = "--MAIN QUERY START";
        public string Query { get; set; }
        public string DeclareBlock
        {
            get
            {
                if (string.IsNullOrEmpty(_declareBlock))
                    _declareBlock = GetDeclareBlock();
                return _declareBlock;
            }
            set
            {
                _declareBlock = value;
            }
        }
        public Dictionary<string, string> SelectColumns
        {
            get
            {
                if (_selectColumns == null || !_selectColumns.Any())
                    _selectColumns = GetSelectColumns();
                return _selectColumns;
            }
            set
            {
                _selectColumns = value;
            }
        }
        public string FromBlock
        {
            get
            {
                if (string.IsNullOrEmpty(_fromBlock))
                    _fromBlock = GetFromBlock(Query);
                return _fromBlock;
            }
            set
            {
                _fromBlock = value;
            }
        }
        public string WhereBlock { get; set; }
        public string GroupByBlock { get; set; }
        public string OrderByBlock { get; set; }

        private string _declareBlock { get; set; }
        private Dictionary<string, string> _selectColumns { get; set; }
        private string _fromBlock { get; set; }
        private string _whereBlock { get; set; }
        private string _groupByBlock { get; set; }
        private string _orderByBlock { get; set; }

        /// <summary>
        /// Assumes that columns are aliased with ' AS '
        /// </summary>
        /// <param name="Query"></param>
        /// <returns>
        /// List of KeyValuePairs where the key is the column alias
        /// and the value is the portion of the select for the specified alias including the AS (alias) but not an ending comma.
        /// </returns>
        protected virtual Dictionary<string, string> GetSelectColumns()
        {
            if (Query == string.Empty || !Query.Contains(' '))
                return null;
            var offset = GetSelectOffset();
            var fromIndex = FindClosingFromIndex(offset, Query);

            //this skips the SELECT at the beginning
            var selectBlock = Query.Substring(offset, fromIndex - offset);
            var selectColumns = new Dictionary<string, string>();
            var index = 0;
            var startIndex = 0;
            while (index + 1 < selectBlock.Length)
            {
                startIndex = index + 1;
                var asIndex = selectBlock.IndexOf(" AS ", index, StringComparison.OrdinalIgnoreCase);
                if (asIndex == -1)
                    break;
                index = selectBlock.IndexOf(",", asIndex) > 1 ? selectBlock.IndexOf(",", asIndex) : selectBlock.Length - 1;
                var key = selectBlock.Substring(asIndex + 4, index - asIndex - 4).Trim();
                var value = selectBlock.Substring(startIndex, index - startIndex).Trim();
                selectColumns.Add(key, value);
            }
            return selectColumns;
        }

        /// <summary>
        /// Find the index at the end of the first SELECT or SELECT DISTINCT after the MainQueryStartIndicator if it is included.
        /// </summary>
        /// <param name="Query"></param>
        /// <returns></returns>
        protected virtual int GetSelectOffset()
        {
            if (Query == string.Empty || !Query.Contains(' '))
                return 0;
            var declareBlock = GetDeclareBlock();
            var mainQuery = Query.Substring(declareBlock.Length);
            var hasDistinct = mainQuery.Split(' ')[1].StartsWith("DISTINCT", StringComparison.OrdinalIgnoreCase);
            return hasDistinct ?
                mainQuery.IndexOf("DISTINCT", StringComparison.OrdinalIgnoreCase) + 8 + declareBlock.Length
                : mainQuery.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase) + 6 + declareBlock.Length;
        }

        /// <summary>
        /// Find any declarations made at the start of a query.
        /// </summary>
        /// <param name="Query"></param>
        /// <returns></returns>
        protected virtual string GetDeclareBlock()
        {
            return Query.IndexOf(MainQueryStartIndicator) > 0 ? Query.Substring(0, Query.IndexOf(MainQueryStartIndicator) + MainQueryStartIndicator.Length) : string.Empty;
        }

        /// <summary>
        /// Given a query from a SELECT clause that is linked to the main query through its WHERE clause, identify and return the linking AND clause.
        /// N.B. Column references in the WHERE clause should include the Alias even if it is not required by SQL.
        /// </summary>
        /// <param name="Query">Subquery from a SELECT statement that may link to the main query through a comparison in the WHERE clause.</param>
        /// <param name="fromIndex">Optional parameter to specify the index of the main FROM clause. Can help to avoid issues with futher subqueries in the SELECT of this subquery.</param>
        /// <returns>
        /// KeyValuePair where the value is a list of AND clauses that do not link to the outside query,
        /// and the key is a list of KeyValuePairs where the key is the local alias and the value is the external query alias for comparisons that link the query to the main.
        /// Note that the actual comparator is lost in this implementation and is assumed to be '='.
        /// </returns>
        protected virtual KeyValuePair<List<KeyValuePair<string, string>>, List<string>> ExtractLinkAndClause(int fromIndex = 0)
        {
            var whereIndex = Query.IndexOfExact("WHERE", fromIndex, true);
            if (whereIndex == -1)
                return new KeyValuePair<List<KeyValuePair<string, string>>, List<string>>();

            //Find all of the aliases for tables in this query.
            var aliases = GetTableAliases(Query, 0);

            var clauses = new List<string>();

            //extract the where clause and break it into comparisons.
            var whereClause = Query.Substring(whereIndex + 5);
            var groupByIndex = whereClause.IndexOfExact("GROUP BY", true);

            if (groupByIndex > 0)
                whereClause = whereClause.Substring(0, groupByIndex);

            var splitByAnd = whereClause.ToUpper().Split(new[] { "AND " }, StringSplitOptions.None);

            var linkComparison = new List<KeyValuePair<string, string>>();

            foreach (var clause in splitByAnd)
            {
                //break the comparison into text elements
                var splitOnSpaces = clause.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                string outerAlias = null;
                string innerAlias = null;
                foreach (var entry in splitOnSpaces)
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

            foreach (var join in joined)
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

            var split = join.ToUpper().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            var aliasIndex = 0;

            for (aliasIndex = 0; aliasIndex < split.Length; aliasIndex++)
            {
                if (split[aliasIndex] == "ON" || split[aliasIndex] == "WITH")
                    break;
            }

            return split[aliasIndex - 1];
        }

        protected virtual string GetFromBlock(string query, int fromIndex = 0)
        {
            var whereIndex = query.IndexOfExact("WHERE", fromIndex, true);
            return whereIndex > fromIndex ? query.Substring(fromIndex, whereIndex - fromIndex) : query.Substring(fromIndex);
        }



    }
}
