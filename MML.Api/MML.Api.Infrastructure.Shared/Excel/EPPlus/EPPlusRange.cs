using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using OfficeOpenXml;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusRange : IRange
    {
        private readonly ExcelRange _range;

        public EPPlusRange(ExcelRange range)
        {
            _range = range;
        }

        IRange IRange.this[int row, int col]
        {
            get { return new EPPlusRange(_range[row, col]); }
        }

        IRange IRange.this[int startRow, int startCol, int endRow, int endCol]
        {
            get { return new EPPlusRange(_range[startRow,startCol,endRow,endCol]);}
        }

        IRange IRange.this[string address]
        {
            get { return new EPPlusRange(_range[address]); }
        }

        public IRangeStyle Style
        {
            get { return new EPPlusStyle(_range.Style); }
        }

        public string GetRangeAddress()
        {
            return _range.Address;
        }

        public object Value
        {
            get { return _range.Value; }
            set { _range.Value = value; }
        }

        public void LoadValuesFromArrays(IEnumerable<object[]> data)
        {
            _range.LoadFromArrays(data);
        }
        public string Formula
        {
            get { return _range.Formula; }
            set { _range.Formula = value; }
        }
        public bool Merged
        {
            get { return _range.Merge; }
            set { _range.Merge = value; }
        }

        public IList<KeyValuePair<string,string>> FindByValue(string value)
        {
            return (from cell in _range
                    where cell.Value != null && cell.Value.ToString().Contains(value)
                    select new KeyValuePair<string, string>(cell.FullAddress, cell.GetValue<string>())).ToList();
        }

        public int StartRow { get { return _range.Start.Row; }}
        public int StartCol { get { return _range.Start.Column; } }

        public string Text {get  { return _range.Text; } }

        public void AutoFitColumns()
        {
            var addressArr = _range.Address.Split(':');
            string strColumnLetter = Regex.Replace(addressArr[0], "[0-9]", "");
            string endColumnLetter = Regex.Replace(addressArr[1], "[0-9]", "");

            int strColumnNumber = ColumnLetterToNumber(strColumnLetter);
            int endColumnNumber = ColumnLetterToNumber(endColumnLetter);

            for (int i = strColumnNumber; i < endColumnNumber; i++)
            {
                ExcelRange columnCells =
                    _range.Worksheet.Cells[
                        _range.Worksheet.Dimension.Start.Row, i, _range.Worksheet.Dimension.End.Row, i];

                var cellsToSearch = columnCells.Where(c => !c.Merge && c.Value != null && !c.Value.ToString().Contains("<r xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">"));
                if (!cellsToSearch.Any())
                    continue;

                int maxLength = cellsToSearch.Max(cell => cell.Value.ToString().Length);                

                if (_range.Worksheet.Column(i).Style.Numberformat.Format.Contains("#,##"))
                {
                    _range.Worksheet.Column(i).Width = maxLength + maxLength/3;
                }
                else
                {
                    _range.Worksheet.Column(i).Width = maxLength;
                }
            }
        }

        
        private static int ColumnLetterToNumber(string columnName)
        {
            if(string.IsNullOrEmpty(columnName)) throw new ArgumentException("columnName ");

            columnName = columnName.ToUpperInvariant();

            int sum = 0;

            for (int i = 0; i < columnName.Length; i++)
            {
                sum *= 26;
                sum += (columnName[i] - 'A' + 1);
            }
            return sum;
        }
    }
}
