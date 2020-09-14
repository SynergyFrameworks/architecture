using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace MML.Enterprise.Excel.EPPlus
{
    public class EPPlusWorksheet : IWorksheet
    {
        private ExcelWorksheet _worksheet;        
        public EPPlusWorksheet(ExcelWorksheet worksheet)
        {
            _worksheet = worksheet;            
        }
        public string Name
        {
            get { return _worksheet.Name; }
            set { _worksheet.Name = value; }
        }

        public bool Hidden
        {
            get 
            {
                return _worksheet.Hidden != OfficeOpenXml.eWorkSheetHidden.Visible;
            }
            set
            {
                if (value)
                {
                    _worksheet.Hidden = OfficeOpenXml.eWorkSheetHidden.Hidden;
                }
                else
                {
                    _worksheet.Hidden = OfficeOpenXml.eWorkSheetHidden.Visible;
                }
            }
        }
        public double DefaultRowHeight
        {
            get { return _worksheet.DefaultRowHeight; }
            set { _worksheet.DefaultRowHeight = value; }
        }
        public double DefaultColWidth
        {
            get { return _worksheet.DefaultColWidth; }
            set { _worksheet.DefaultColWidth = value; }
        }
        public IRange Cells { get { return new EPPlusRange(_worksheet.Cells); } }
        public IRange SelectedRange { get { return new EPPlusRange(_worksheet.SelectedRange); } }
        public IDrawings Drawings { get { return new EPPlusDrawings(_worksheet.Drawings); } }

        public void LockWorksheet(string password)
        {
            _worksheet.Protection.SetPassword(password);
            _worksheet.Protection.IsProtected = true;
        }

        public void UnlockWorksheet(string password)
        {
            _worksheet.Protection.SetPassword(password);
            _worksheet.Protection.IsProtected = false;
        }

        public IHeaderFooter HeaderFooter
        {
            get { return new EPPlusHeaderFooter(_worksheet.HeaderFooter); }
        }

        public void InsertRow(int rowFrom, int numRows)
        {
            _worksheet.InsertRow(rowFrom,numRows);
        }

        public IRange UsedRange { get
        {
                if (_worksheet.Dimension == null)
                {
                    return null;
                }
                return new EPPlusRange(_worksheet.Cells[_worksheet.Dimension.Start.Row, _worksheet.Dimension.Start.Column, _worksheet.Dimension.End.Row, _worksheet.Dimension.End.Column]);
        } }

        public List<ExcelNamedRange> GetNames()
        {
            List<ExcelNamedRange> names = new List<ExcelNamedRange>();
            foreach(ExcelNamedRange name in _worksheet.Workbook.Names)
            {
                names.Add(name);
            }
            foreach (ExcelNamedRange name in _worksheet.Names)
            {
                names.Add(name);
            }

            return names;
        }

        public ExcelNamedRange GetNamedRange(string name)
        {
            ExcelNamedRange return_value = null;
            if (_worksheet.Names.ContainsKey(name))
            {
                return_value = _worksheet.Names[name];
            }
            else
            {
                if (_worksheet.Workbook.Names.ContainsKey(name))
                    return_value = _worksheet.Workbook.Names[name];
            }
            return return_value;
        }

        public List<ExcelTable> GetTables()
        {
            return _worksheet.Tables?.ToList();
        }

        public ExcelTable GetTable(string name)
        {
            return _worksheet.Tables?.FirstOrDefault(t => t.Name == name);
        }

        public IList<KeyValuePair<string, string>> FindWithValue(string value)
        {
            if(_worksheet.Dimension == null)
                return new List<KeyValuePair<string, string>>();
            return
                (from cell in
                     _worksheet.Cells[
                         _worksheet.Dimension.Start.Row, _worksheet.Dimension.Start.Column, _worksheet.Dimension.End.Row,
                         _worksheet.Dimension.End.Column]
                 where cell.Value != null &&  cell.Value.ToString().Contains(value)
                 select new KeyValuePair<string,string>(cell.FullAddress,cell.GetValue<string>())).ToList();
        }

        public void AddDataValidation(string address, string formula)
        {
            var validation = _worksheet.DataValidations.AddListValidation(address);
            validation.Formula.ExcelFormula = formula;
        }

        public void Activate(bool selectSheet)
        {
            _worksheet.Select();
        }
    }
}
