using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Common.Mail
{
    public abstract class AbstractMailManager:IMailManager
    {
        public virtual void SendEmail(IList<string> recipients, string subject, string content)
        {
            throw new NotImplementedException();
        }

        public virtual void SendEmail(Dictionary<string, Object> vars, string subject, string content)
        {
            throw new NotImplementedException();
        }

        public virtual void SendTemplateEmail(Dictionary<string, object> vars, string subject, string templateName, object dataObject)
        {
            throw new NotImplementedException();
        }
    }
}
