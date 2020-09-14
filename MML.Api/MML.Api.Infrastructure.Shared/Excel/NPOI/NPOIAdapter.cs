using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.HSSF.UserModel;

namespace MML.Enterprise.Excel.NPOI
{
    public class NPOIAdapter : IExcelAdapter
    {
        public IWorkbook OpenWorkbook(FileInfo file)
        {
            IWorkbook workbook;
            using (var fs = new FileStream(file.FullName, FileMode.Open))
            {
                workbook = new NPOIWorkbook(new HSSFWorkbook(fs));
            }

            return workbook;
        }
    }
}
