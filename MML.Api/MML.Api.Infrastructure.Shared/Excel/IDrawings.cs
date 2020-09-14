using OfficeOpenXml.Drawing.Chart;
using System.Collections.Generic;
using System.IO;

namespace MML.Enterprise.Excel
{
    public interface IDrawings : IEnumerable<IDrawing>
    {
        int Count { get; }
        IDrawing this[int position] { get; }
        IDrawing this[string Name] { get; }

        IPicture AddPicture(string name, FileInfo file); 
        IChart AddChart(string name, eChartType chartType);

        void Remove(int index);
        void Remove(string name);
    }
}
