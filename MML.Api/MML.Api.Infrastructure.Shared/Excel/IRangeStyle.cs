using System.Security.Cryptography.X509Certificates;
using OfficeOpenXml.Style;

namespace MML.Enterprise.Excel
{
    public interface IRangeStyle
    {
        bool Bold { get; set; }
        IFill Fill { get; }
        string NumberFormat { get; set; }
        ExcelStyle Style { get;}
    }
}
