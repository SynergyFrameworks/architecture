using OfficeOpenXml.Drawing.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Excel
{
    public interface IChartLegend
    {
        eLegendPosition Position { get; set; }
        void Remove();
    }
}
