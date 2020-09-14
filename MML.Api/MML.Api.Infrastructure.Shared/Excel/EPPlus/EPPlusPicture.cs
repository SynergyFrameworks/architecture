using OfficeOpenXml.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusPicture: EPPlusDrawing, IPicture
    {
        private readonly ExcelPicture _picture;
        public EPPlusPicture(ExcelPicture picture)
        {
            _picture = picture;
        }
    }
}
