using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Persistence.Dapper.TypeHandlers
{
    public class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override DateTime Parse(object value)
        {
            var dateTime = DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
            
            if (dateTime.TimeOfDay == TimeSpan.Zero) {
                dateTime = DateTime.SpecifyKind((DateTime)value, DateTimeKind.Local);
            }

            return dateTime;
        }

        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
