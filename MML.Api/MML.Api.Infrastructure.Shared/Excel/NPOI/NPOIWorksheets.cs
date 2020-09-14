using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace MML.Enterprise.Excel.NPOI
{
    public class NPOIWorksheets : IWorksheets
    {
        private global::NPOI.SS.UserModel.IWorkbook _workbook;

        public NPOIWorksheets(global::NPOI.SS.UserModel.IWorkbook workbook)
        {
            _workbook = workbook;
        }
        public IEnumerator<IWorksheet> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return _workbook.NumberOfSheets; }            
        }

        IWorksheet IWorksheets.this[int position]
        {
            get { return new NPOIWorksheet(_workbook.GetSheetAt(position)); }
        }

        IWorksheet IWorksheets.this[string name]
        {
            get { return new NPOIWorksheet(_workbook.GetSheet(name)); }
        }

        public IWorksheet Add(string name)
        {
            return new NPOIWorksheet(_workbook.CreateSheet(name));
        }

        public void Remove(string name)
        {
            _workbook.RemoveSheetAt(_workbook.GetSheetIndex(name));
        }

        public IWorksheet Copy(string existingWorksheet, string newWorksheet)
        {
            var sheet = (HSSFSheet)_workbook.GetSheet(existingWorksheet);

            sheet.CopyTo((HSSFWorkbook)_workbook,newWorksheet,true,true);

            return new NPOIWorksheet(_workbook.GetSheet(newWorksheet));            
        }
    }
}
