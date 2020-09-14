using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusChartSeries: IChartSeries
    {
        public ExcelChartSeries _series;
        public EPPlusChartSeries(ExcelChartSeries series)
        {
            _series = series;
        }

        IChartSerie IChartSeries.this[int position]
        {
            get { return new EPPlusChartSerie(_series[position]); }
        }

        public virtual IChartSerie Add(string serieAddress, string xSerieAddress)
        {
            return new EPPlusChartSerie(_series.Add(serieAddress, xSerieAddress));
        }

        public virtual IChartSerie Add(ExcelRangeBase serie, ExcelRangeBase xSerie) 
        {
            return new EPPlusChartSerie(_series.Add(serie, xSerie));
        }

        public void Delete(int position) 
        {
            _series.Delete(position);
        }
    }
}
