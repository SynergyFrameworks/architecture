using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusChart: EPPlusDrawing, IChart
    {
        private readonly ExcelChart _chart;

        public EPPlusChart(ExcelChart chart)
        {
            _chart = chart;
        }

        public string Title
        {
            get { return _chart.Title.Text; }
            set { _chart.Title.Text = value; }
        }

        public IChartLegend Legend
        {
            get 
            {
                return new EPPlusChartLegend(_chart.Legend);
            }
        }

        //public bool RoundedCorners 
        //{
        //    get { return _chart.RoundedCorners; }
        //    set { _chart.RoundedCorners = value; }
        //}

        public IChartSeries Series
        {
            get
            {
                return new EPPlusChartSeries(_chart.Series);
            }
        }

        public bool RoundedCorners { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        //public void RemoveYAxisGridLines(bool removeMajor, bool removeMinor) {
        //    _chart.YAxis.RemoveGridlines(removeMajor, removeMinor);
        //}

        //public void RemoveXAxisGridLines(bool removeMajor, bool removeMinor)
        //{
        //    _chart.XAxis.RemoveGridlines(removeMajor, removeMinor);
        //}

        public void RemoveTickMarks()
        {
            _chart.XAxis.MajorTickMark = eAxisTickMark.None;
            _chart.XAxis.MinorTickMark = eAxisTickMark.None;

            _chart.YAxis.MajorTickMark = eAxisTickMark.None;
            _chart.YAxis.MinorTickMark = eAxisTickMark.None;
        }

        public void RemoveXAxisGridLines(bool removeMajor, bool removeMinor)
        {
            throw new NotImplementedException();
        }

        public void RemoveYAxisGridLines(bool removeMajor, bool removeMinor)
        {
            throw new NotImplementedException();
        }
    }
}
