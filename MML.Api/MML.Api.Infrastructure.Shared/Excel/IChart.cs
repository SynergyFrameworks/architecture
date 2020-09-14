using OfficeOpenXml.Drawing.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Excel
{
    public interface IChart: IDrawing
    {
        string Title { get; set; }
        bool RoundedCorners { get; set; }
        IChartSeries Series { get; }
        IChartLegend Legend { get; }
        void RemoveYAxisGridLines(bool removeMajor, bool removeMinor);
        void RemoveXAxisGridLines(bool removeMajor, bool removeMinor);
        void RemoveTickMarks();
    }
}
