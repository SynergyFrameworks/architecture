using System;

namespace MML.Enterprise.Common.Mapping
{
    public interface IDynamicColumnValue
    {
        string ColumnHeader { get; set; }
        decimal? NumberValue { get; set; }
        DateTime? DateValue { get; set; }
        string StringValue { get; set; }
    }
}
