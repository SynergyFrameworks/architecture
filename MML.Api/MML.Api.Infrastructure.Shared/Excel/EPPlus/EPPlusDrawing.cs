using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusDrawing : IDrawing
    {
        public EPPlusDrawing() {
            
        }

        private readonly ExcelDrawing _drawing;
        public EPPlusDrawing(ExcelDrawing drawing)
        {
            _drawing = drawing;
        }

        public void SetPosition(int top, int left)
        {
            _drawing.SetPosition(top, left);
        }

        public void SetPosition(int row, int rowOffsetPixels, int column, int columnOffsetPixels)
        {
            _drawing.SetPosition(row, rowOffsetPixels, column, columnOffsetPixels);
        }

        public void SetSize(int percent)
        {
            _drawing.SetSize(percent);
        }

        public void SetSize(int pixelWidth, int pixelHeight)
        {
            _drawing.SetSize(pixelWidth, pixelHeight);
        }
    }
}
