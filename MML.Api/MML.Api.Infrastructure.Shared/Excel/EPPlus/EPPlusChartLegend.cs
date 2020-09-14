using OfficeOpenXml.Drawing.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusChartLegend: IChartLegend
    {
        private ExcelChartLegend _legend;
        public EPPlusChartLegend(ExcelChartLegend legend)
        {
            _legend = legend;
        }

        public eLegendPosition Position
        {
            get { return _legend.Position; }
            set { _legend.Position = value; }
        }

        public void Remove() {
            _legend.Remove();
        }
    }
}
