using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Excel.NPOI
{
    public class NPOIWorkbook : IWorkbook
    {
        private global::NPOI.SS.UserModel.IWorkbook _workbook;
        public NPOIWorkbook(global::NPOI.SS.UserModel.IWorkbook workbook)
        {
            _workbook = workbook;
        }

        public void Save()
        {
            SaveAs(Path.GetTempFileName());            
        }

        public void SaveAs(string uri)
        {
            using (var fileStream = new FileStream(uri, FileMode.CreateNew))
            {
                _workbook.Write(fileStream);
            }
        }

        public IWorksheet AddWorksheet(string worksheetName)
        {
            throw new NotImplementedException();
        }

        public IWorksheets Worksheets { get; private set; }

        public void ActivateWorksheet(int worksheetIndex) {
            _workbook.SetActiveSheet(worksheetIndex);
        }
    }
}
