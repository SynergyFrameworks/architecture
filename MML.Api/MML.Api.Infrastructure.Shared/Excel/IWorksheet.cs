using OfficeOpenXml;
using OfficeOpenXml.Table;
using System.Collections.Generic;

namespace MML.Enterprise.Excel
{
    public interface IWorksheet
    {
        string Name { get; set; }
        bool Hidden { get; set; }
        double DefaultRowHeight { get; set; }
        double DefaultColWidth { get; set; }
        IRange Cells { get; }
        IRange SelectedRange { get; }
        IDrawings Drawings { get; }
        void LockWorksheet(string password);
        void UnlockWorksheet(string password);
        IHeaderFooter HeaderFooter { get;}
        void InsertRow(int rowFrom, int numRows);
        IRange UsedRange { get; }
        List<ExcelNamedRange> GetNames();
        ExcelNamedRange GetNamedRange(string name);
        List<ExcelTable> GetTables();
        ExcelTable GetTable(string name);
        IList<KeyValuePair<string, string>> FindWithValue(string value);

        /// <summary>
        /// Add data validation to a cell (drop-down list)
        /// </summary>
        /// <param name="address">Cell Address</param>
        /// <param name="formula">drop down list formula</param>
        void AddDataValidation(string address, string formula);
        void Activate(bool selectSheet = true);
    }
}
