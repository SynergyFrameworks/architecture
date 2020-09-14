using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Excel
{
    public interface IChartSeries
    {
        IChartSerie this[int position] { get; }
        IChartSerie Add(string serieAddress, string xSerieAddress);
        IChartSerie Add(ExcelRangeBase serie, ExcelRangeBase xSerie);
        void Delete(int position);
    }
}
