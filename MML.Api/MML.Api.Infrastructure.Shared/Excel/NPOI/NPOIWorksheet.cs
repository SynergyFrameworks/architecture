using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using MML.Enterprise.Excel.EPPlus;

namespace MML.Enterprise.Excel.NPOI
{
    public class NPOIWorksheet : IWorksheet
    {
        private ISheet _workSheet;

        public NPOIWorksheet(ISheet workSheet)
        {
            _workSheet = workSheet;
        }

        public string Name
        {
            get { return _workSheet.SheetName; }
            set { _workSheet.Workbook.SetSheetName(Index, value); }
        }

        public bool Hidden
        {
            get { return _workSheet.Workbook.IsSheetHidden(Index); }
            set
            {
                if(value)
                    _workSheet.Workbook.SetSheetHidden(Index,1);
                else
                {
                    _workSheet.Workbook.SetSheetHidden(Index, 0);
                }
            }
        }

        public double DefaultRowHeight { get; set; }
        public double DefaultColWidth { get; set; }

        public IRange Cells
        {
            get
            {
                var firstRow = _workSheet.GetRow(0);
                return new NPOIRange(CellRangeAddress.ValueOf("A1:"+ExcelHelper.GetExcelColumnFromNumber(firstRow.LastCellNum)+_workSheet.LastRowNum), _workSheet);
            }
        }

        public IRange SelectedRange { get; private set; }
        public IDrawings Drawings { get; private set; }
        public void LockWorksheet(string password)
        {
            _workSheet.ProtectSheet(password);            
        }

        public void UnlockWorksheet(string password)
        {            
            throw new NotImplementedException();
        }

        public IHeaderFooter HeaderFooter { get; private set; }
        public void InsertRow(int rowFrom, int numRows)
        {
            for (var i = rowFrom; i < rowFrom + numRows; i++)
            {
                _workSheet.CreateRow(i);
            }            
        }

        public IRange UsedRange
        {
            get
            {
                var firstRow = _workSheet.GetRow(0);
                return new NPOIRange(CellRangeAddress.ValueOf("A1:" + ExcelHelper.GetExcelColumnFromNumber(firstRow.LastCellNum) + _workSheet.LastRowNum), _workSheet);
            }
        }
        public List<ExcelNamedRange> GetNames()
        {
            throw new NotImplementedException();
        }

        public ExcelNamedRange GetNamedRange(string name)
        {
            throw new NotImplementedException();
        }
        public List<ExcelTable> GetTables()
        {
            throw new NotImplementedException();
        }

        public ExcelTable GetTable(string name)
        {
            throw new NotImplementedException();
        }

        public IList<KeyValuePair<string, string>> FindWithValue(string value)
        {
            var results = new List<KeyValuePair<string, string>>();
            for (var i = 0; i < _workSheet.LastRowNum; i++)
            {
                var row = _workSheet.GetRow(i);
                for (var j = 0; j < row.LastCellNum; j++)
                {
                    var cell = row.GetCell(j);
                    if (cell.StringCellValue.Contains(value))
                    {
                        results.Add(new KeyValuePair<string, string> ());   
                    }
                }
            }

            return results;

        }

        public void AddDataValidation(string address, string formula)
        {
            throw new NotImplementedException();
        }

        private int Index
        {
            get { return _workSheet.Workbook.GetSheetIndex(_workSheet); }
        }

        public void Activate(bool selectSheet)
        {
            _workSheet.SetActive(selectSheet);
        }
    }
}
