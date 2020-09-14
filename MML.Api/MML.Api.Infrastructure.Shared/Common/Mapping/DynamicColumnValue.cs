using System;

namespace MML.Enterprise.Common.Mapping
{
    public class DynamicColumnValue : IDynamicColumnValue
    {
        public string ColumnHeader { get; set; }
        public decimal? NumberValue { get; set; }
        public DateTime? DateValue { get; set; }
        private string _stringValue { get; set; }
        public string StringValue
        {
            get
            {
                if (string.IsNullOrEmpty(_stringValue) && NumberValue != null)
                    _stringValue = NumberValue.Value.ToString();
                else if (string.IsNullOrEmpty(_stringValue) && DateValue != null)
                    _stringValue = DateValue.Value.ToString();
                return _stringValue;
            }
            set
            {
                _stringValue = value;
            }
        }
    }
}
