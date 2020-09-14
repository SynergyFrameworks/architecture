using System.Collections.Generic;

namespace MML.Enterprise.Excel
{
    public static class WorksheetExtensions
    {
        /// <summary>
        /// Merges common cells in a worksheet. This works by finding a range of common cells and merge them. Note: given that the indexes provided
        /// will reference data on an Excel worksheet, the indexes will need to be based on an index origin of one.
        /// </summary>
        /// <param name="worksheet">The worksheet which will have its cells merged.</param>
        /// <param name="startRow">The first row of data on the excel sheet. Index origin of one.</param>
        /// <param name="endRow">The last row of data on the excel sheet. Index origin of one.</param>
        /// <param name="checkColumnIndex">This index references a single column which will be used when checking for a unique set of rows which can be merged.</param>
        /// <param name="mergeColumnIndexes">The list of columns which should have its values merged if common values are found. Index origin of one.</param>
        public static void MergeCells(this IWorksheet worksheet, int startRow, int endRow, int checkColumnIndex, IEnumerable<int> mergeColumnIndexes)
        {
            // Attempt to loop through the following cells to determine if they can be merged.
            // If they can, find the last cell to be merged
            for (var i = startRow; i <= endRow; i++)
            {
                var j = i + 1;
                while (j <= endRow)
                {
                    // Determine if the row of cells in row i is the same as the row of cells in row j
                    // If they are, then iterate j and continue, otherwise break out of the while loop
                    var baseCellValue = worksheet.Cells[i, checkColumnIndex].Value;
                    var currentCellValue = worksheet.Cells[j, checkColumnIndex].Value;

                    // If one value is null while the other is not, break.
                    // If they are both not null yet their values don't match, break
                    // Otherwise they match, so continue.
                    if (baseCellValue == null && currentCellValue != null ||
                        baseCellValue != null && currentCellValue == null ||
                        baseCellValue != null && baseCellValue.ToString() != currentCellValue.ToString())
                    {
                        break;
                    }

                    j++;
                }

                // If j is i+1, then we didn't find any cells to be merged
                if (j == i + 1) continue;

                // Otherwise, we did find a range which can be merged.
                // Therefore, merge each prescribed column passed in as a parameter
                foreach(var index in mergeColumnIndexes)
                {
                    worksheet.Cells[i, index, j - 1, index].Merged = true;
                }

                // Update iterator before continuing
                i = j - 1;
            }
        }
    }
}
