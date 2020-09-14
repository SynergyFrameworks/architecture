using System;
using System.IO;
using NPOI.SS.Formula.Functions;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusAdapter : IExcelAdapter
    {
        public IWorkbook OpenWorkbook(FileInfo file)
        {
            IWorkbook workbook;
            try
            {
                if (file.Exists && file.Length == 0)
                {
                    file.Delete();
                    workbook = new EPPlusWorkbook(file);
                    workbook.Worksheets.Add("Sheet 1");
                }
                else
                {
                    workbook = new EPPlusWorkbook(file);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Invalid Excel file specified");
            }

            if(workbook.Worksheets.Count <1)
                throw new InvalidDataException("Invalid file specified, no worksheets found");

            return workbook;
        }
    }
}

