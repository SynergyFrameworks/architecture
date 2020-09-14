using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml.Style;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusFill : IFill
    {
        private ExcelFill _fill;
        public EPPlusFill(ExcelFill fill)
        {
            _fill = fill;            
        }
        public FillTypes FillType { get { return (FillTypes)Enum.Parse(typeof(FillTypes), _fill.PatternType.ToString()); } set { } }
        public Color Color
        {
            get { return new Color(); }
            set
        {
            _fill.PatternType = ExcelFillStyle.Solid;
            _fill.BackgroundColor.SetColor(value);
        } }
    }
}
