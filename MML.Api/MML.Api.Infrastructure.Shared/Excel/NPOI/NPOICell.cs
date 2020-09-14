using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.SS.UserModel;

namespace MML.Enterprise.Excel.NPOI
{
    public class NPOICell : IRange
    {
        private ICell _cell;
        public NPOICell(ICell cell)
        {
            _cell = cell;
        }
        IRange IRange.this[int row, int col]
        {
            get { throw new NotImplementedException(); }
        }

        IRange IRange.this[int startRow, int startCol,int endRow,int endCol]
        {
            get { throw new NotImplementedException();}
        }

        IRange IRange.this[string address]
        {
            get { throw new NotImplementedException(); }
        }

        public IRangeStyle Style { get; private set; }

        public object Value
        {
            get { return _cell.StringCellValue; }
            set
            {
                _cell.SetCellValue(value.ToString());
            }
        }

        public string Formula { get; set; }
        public bool Merged { get; set; }
        public IList<KeyValuePair<string, string>> FindByValue(string value)
        {
            throw new NotImplementedException();
        }

        public int StartRow { get; private set; }
        public int StartCol { get; private set; }

        public string Text { get { return _cell.StringCellValue; } }

        public void LoadValuesFromArrays(IEnumerable<object[]> data)
        {
            throw new NotImplementedException();
        }

        public void AutoFitColumns()
        {
            throw new NotImplementedException();
        }

        public string GetRangeAddress()
        {
            throw new NotImplementedException();
        }
    }
}
