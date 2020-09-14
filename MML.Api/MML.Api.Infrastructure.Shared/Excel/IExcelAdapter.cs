using System.IO;

namespace MML.Enterprise.Excel
{
    public interface IExcelAdapter
    {
        IWorkbook OpenWorkbook(FileInfo file);
    }
}
