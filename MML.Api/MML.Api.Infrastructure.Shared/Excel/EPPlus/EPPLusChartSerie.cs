using OfficeOpenXml.Drawing.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusChartSerie: IChartSerie
    {
        private ExcelChartSerie _serie;
        public EPPlusChartSerie(ExcelChartSerie serie)
        {
            _serie = serie;
        }

        public string Header
        {
            get { return _serie.Header; }
            set { _serie.Header = value; }
        }

    }
}
