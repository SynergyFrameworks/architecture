using System.IO;
using OfficeOpenXml;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusWorkbook : IWorkbook
    {
        private readonly ExcelPackage _package;

        public EPPlusWorkbook(FileInfo file)
        {            
            _package = new ExcelPackage(file);
        }
        public void Save()
        {
            _package.Save();
        }

        public void SaveAs(string uri)
        {
            var file = new FileInfo(uri);
            _package.SaveAs(file);
        }

        public IWorksheets Worksheets
        {
            get
            {
                return new EPPlusWorksheets(_package.Workbook.Worksheets);
            }
        }

        public IWorksheet AddWorksheet(string worksheetName)
        {
            return new EPPlusWorksheet(_package.Workbook.Worksheets.Add(worksheetName));
        }

        public void ActivateWorksheet(int worksheetIndex)
        {
            _package.Workbook.View.ActiveTab = worksheetIndex;
        }
    }
}
