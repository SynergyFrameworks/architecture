using System;

namespace MML.Enterprise.Excel
{
    public static class ExcelHelper
    {
        public static string GetExcelColumnFromNumber(int colNum)
        {
            var dividend = colNum;
            var columnName = String.Empty;

            while (dividend > 0)
            {
                var modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = ((dividend - modulo) / 26);
            }

            return columnName;
        }
        //http://stackoverflow.com/questions/4583191/incrementation-of-char
        public static int GetColNumberFromName(string columnName)
        {
            var characters = columnName.ToUpperInvariant().ToCharArray();
            var sum = 0;
            foreach (var t in characters)
            {
                sum *= 26;
                sum += (t - 'A' + 1);
            }
            return sum;
        }
    }
}
