namespace MML.Enterprise.Excel
{
    public interface IWorkbook
    {
        void Save();
        void SaveAs(string uri);
        IWorksheets Worksheets { get; }
        IWorksheet AddWorksheet(string worksheetName);
        void ActivateWorksheet(int worksheetIndex);
    }
}
