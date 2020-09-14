using OfficeOpenXml;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusHeaderFooter : IHeaderFooter
    {
        private readonly ExcelHeaderFooter _excelHeaderFooter;

        public EPPlusHeaderFooter(ExcelHeaderFooter excelHeaderFooter)
        {
            _excelHeaderFooter = excelHeaderFooter;
            _excelHeaderFooter.differentOddEven = false;
            _excelHeaderFooter.differentFirst = false;
        }
        public IHeaderFooterText Header
        {
            get { return new EPPlusHeaderFooterText(_excelHeaderFooter.OddHeader); }
        }

        public IHeaderFooterText Footer
        {
            get { return new EPPlusHeaderFooterText(_excelHeaderFooter.OddFooter); }
        }

        public bool AlignWithMargins
        {
            get { return _excelHeaderFooter.AlignWithMargins; }
            set { _excelHeaderFooter.AlignWithMargins = value; }
        }
    }
}
