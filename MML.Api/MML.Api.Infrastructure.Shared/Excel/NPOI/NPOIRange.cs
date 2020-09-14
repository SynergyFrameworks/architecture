using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;

namespace MML.Enterprise.Excel.NPOI
{
    public class NPOIRange : IRange
    {
        private ISheet _workSheet;       

        private CellRangeAddress _range;

        public NPOIRange(CellRangeAddress range, ISheet workSheet)
        {            
            _range = range;            
            _workSheet = workSheet;
        }
        IRange IRange.this[int row, int col]
        {
            get { return new NPOICell(_workSheet.GetRow(row).GetCell(col)); }
        }

        IRange IRange.this[int startRow, int startCol, int endRow, int endCol]
        {
            get { throw new NotImplementedException(); }
        }

        IRange IRange.this[string address]
        {
            get { return new NPOIRange(CellRangeAddress.ValueOf(address),_workSheet); }
        }

        public IRangeStyle Style { get; private set; }
        
        //For ranges we're defaulting the first cell for the moment
        public object Value
        {
            get { return NPOICommon.GetCellValue(GetFirstCell); }
            set
            {
                NPOICommon.SetCellValue(GetFirstCell,value);
            }
        }

        public string Formula
        {
            get { return GetFirstCell.CellFormula; }
            set
            {
                GetFirstCell.SetCellFormula(value);
            }
        }

        public bool Merged { get; set; }
        public IList<KeyValuePair<string, string>> FindByValue(string value)
        {
            throw new NotImplementedException();
        }

        public int StartRow { get; private set; }
        public int StartCol { get; private set; }
        public void LoadValuesFromArrays(IEnumerable<object[]> data)
        {
            throw new NotImplementedException();
        }

        public void AutoFitColumns()
        {
            throw new NotImplementedException();
        }

        private ICell GetFirstCell
        {
            get { return _workSheet.GetRow(_range.FirstRow).GetCell(_range.FirstColumn); }
        }

        public string Text { get { return NPOICommon.GetCellValue(GetFirstCell).ToString(); } }

        public string GetRangeAddress()
        {
            throw new NotImplementedException();
        }
    }
}
