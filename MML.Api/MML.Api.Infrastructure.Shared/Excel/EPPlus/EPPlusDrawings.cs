using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusDrawings : IDrawings
    {
        private readonly ExcelDrawings _drawings;
        public EPPlusDrawings(ExcelDrawings drawings)
        {
            _drawings = drawings;
        }

        public int Count
        {
            get { return _drawings.Count; }
        }
                
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IDrawing IDrawings.this[int position]
        {
            get { return new EPPlusDrawing(_drawings[position]); }
        }

        IDrawing IDrawings.this[string name]
        {
            get { return new EPPlusDrawing(_drawings[name]); }
        }

        public IEnumerator<IDrawing> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public IPicture AddPicture(string name, FileInfo file)
        {
            return new EPPlusPicture(_drawings.AddPicture(name, file));
        }

        public IChart AddChart(string name, eChartType chartType)
        {
            return new EPPlusChart(_drawings.AddChart(name, chartType));
        }

        public void Remove(string name) {
            _drawings.Remove(name);
        }
 
        public void Remove(int index) {
            _drawings.Remove(index);
        } 
    }
}
