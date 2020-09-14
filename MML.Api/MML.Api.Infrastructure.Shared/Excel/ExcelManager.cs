using System.IO;

namespace MML.Enterprise.Excel
{
    public class ExcelManager : IExcelManager
    {
        public IExcelAdapter ExcelAdapter { get; set; }

        public IWorkbook OpenWorkbook(string uri)
        {
            var file = new FileInfo(uri);
            return ExcelAdapter.OpenWorkbook(file);
        }
        public IWorkbook OpenWorkbook(FileInfo file)
        {
            return ExcelAdapter.OpenWorkbook(file);
        }
        public IWorkbook OpenWorkbook(Stream stream)
        {
            var temp = new FileInfo(Path.GetTempFileName());
            var file = File.Create(temp.FullName);
            stream.CopyTo(file);
            file.Close();
            return ExcelAdapter.OpenWorkbook(temp);

        }
    }
}
