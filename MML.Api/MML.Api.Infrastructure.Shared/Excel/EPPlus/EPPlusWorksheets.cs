using System;
using System.Collections;
using System.Collections.Generic;
using OfficeOpenXml;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusWorksheets : IWorksheets
    {
        private ExcelWorksheets _worksheets;

        public EPPlusWorksheets(ExcelWorksheets worksheets)
        {
            _worksheets = worksheets;
        }
        public IEnumerator<IWorksheet> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return _worksheets.Count; }
        }

        IWorksheet IWorksheets.this[int position]
        {
            get { return new EPPlusWorksheet(_worksheets[position]); }
        }

        IWorksheet IWorksheets.this[string name]
        {
            get { return new EPPlusWorksheet(_worksheets[name]); }
        }

        public IWorksheet Add(string name)
        {
            return new EPPlusWorksheet(_worksheets.Add(name));
        }

        public void Remove(string name)
        {
            _worksheets.Delete(name);
        }

        public IWorksheet Copy(string existingWorksheet, string newWorksheet)
        {
            return new EPPlusWorksheet(_worksheets.Copy(existingWorksheet, newWorksheet));
        }
    }
}
