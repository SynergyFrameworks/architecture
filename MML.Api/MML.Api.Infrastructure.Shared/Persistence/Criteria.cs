using MML.Enterprise.Common.DataStructures;
using System.Collections.Generic;
namespace MML.Enterprise.Persistence
{
    public class Criteria
    {
        public int? StartPage { get; set; }
        public int? PageSize { get; set; }
        public SortedDictionary<string, object> Parameters { get; set; }
        public SequentialDictionary<string, SortOrder> SortOrders { get; set; }
        public SortedDictionary<string, object> SearchParameters { get; set; }
        public string SearchQuery { get; set; }
        public List<string> DynamicHeaders { get; set; }
        public Criteria()
        {
            Parameters = new SortedDictionary<string, object>();
            SortOrders = new SequentialDictionary<string, SortOrder>();
            SearchParameters = new SortedDictionary<string, object>();
        }
    }
}
