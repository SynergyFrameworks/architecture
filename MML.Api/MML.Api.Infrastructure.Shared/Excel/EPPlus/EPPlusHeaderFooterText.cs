using OfficeOpenXml;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusHeaderFooterText : IHeaderFooterText
    {
        private readonly ExcelHeaderFooterText _headerFooterText;

        public EPPlusHeaderFooterText(ExcelHeaderFooterText headerFooterText)
        {
            _headerFooterText = headerFooterText;
        }
        public string LeftAlignedText
        {
            get { return _headerFooterText.LeftAlignedText; }
            set { _headerFooterText.LeftAlignedText = value; }
        }

        public string CenteredText
        {
            get { return _headerFooterText.CenteredText; }
            set
            {
                _headerFooterText.CenteredText = value;
            }
        }
        public string RightAlignedText
        {
            get { return _headerFooterText.RightAlignedText; }
            set { _headerFooterText.RightAlignedText = value; }
        }
    }
}
