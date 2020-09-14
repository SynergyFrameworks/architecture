using System.Collections.Generic;

namespace MML.Enterprise.Excel
{
    public interface IWorksheets : IEnumerable<IWorksheet>
    {
        int Count { get; }
        IWorksheet this[int position] { get; }
        IWorksheet this[string name] { get; }
        IWorksheet Add(string name);
        void Remove(string name);
        IWorksheet Copy(string existingWorksheet, string newWorksheet);

    }
}
