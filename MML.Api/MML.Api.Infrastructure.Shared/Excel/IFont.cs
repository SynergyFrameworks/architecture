using System.Drawing;

namespace MML.Enterprise.Excel
{
    public interface IFont
    {
        string Name { get; set; }
        int Size { get; set; }
        FontStyle FontStyle { get; set; }
        Color Color { get; set; }
    }
}
