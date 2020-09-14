using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Common.Persistence
{
    public interface IPersistent
    {
        Guid Id { get; set; }
    }
}
