using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.SS.UserModel;

namespace MML.Enterprise.Excel.NPOI
{
    public static class NPOICommon
    {

        public static object GetCellValue(ICell cell)
        {
            try
            {
                return cell.DateCellValue;
            }
            catch
            {
            }
            try
            {
                return cell.NumericCellValue;
            }
            catch { }
            try
            {
                return cell.BooleanCellValue;
            }
            catch
            {
            }
            return cell.StringCellValue;
        }

        public static void SetCellValue(ICell cell, object value)
        {
            if(value == null)
                cell.SetCellValue(string.Empty);
            else if (value is DateTime)           
                cell.SetCellValue((DateTime)value);            
            else if(value is double)
                cell.SetCellValue((double)value);
            else if (value is decimal)
                cell.SetCellValue(Convert.ToDouble((decimal)value));
            else if(value is bool)
                cell.SetCellValue((bool)value);
            else
                cell.SetCellValue(value.ToString());
        }
    }
}
