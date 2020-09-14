using OfficeOpenXml.Style;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusStyle : IRangeStyle
    {
        private ExcelStyle _style;

        public EPPlusStyle(ExcelStyle style)
        {
            _style = style;                      
        }
        public IFill Fill { get { return new EPPlusFill(_style.Fill); } }

        public bool Bold {get { return _style.Font.Bold; }
        set { _style.Font.Bold = value; }}

        public string NumberFormat { get { return _style.Numberformat.Format; } set { _style.Numberformat.Format = value; } }
        public ExcelStyle Style { get { return _style; } }
    }
}
