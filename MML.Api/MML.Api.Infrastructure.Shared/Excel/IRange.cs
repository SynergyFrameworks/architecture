using System.Collections.Generic;

namespace MML.Enterprise.Excel
{
    public interface IRange
    {
        IRange this[int row, int col] { get; }
        IRange this[int startRow, int startCol, int endRow, int endCol] { get; }
        IRange this[string address] { get; }
        IRangeStyle Style { get; }
        object Value { get; set; }
        string Text { get;}
        string Formula { get; set; }
        bool Merged { get; set; }
        /// <summary>
        /// Returns a dictionary of Address/Cell values based on a search term. 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        IList<KeyValuePair<string, string>> FindByValue(string value);
        int StartRow { get; }
        int StartCol { get; }
        void LoadValuesFromArrays(IEnumerable<object[]> data);
        void AutoFitColumns();
        string GetRangeAddress();
    }
}
