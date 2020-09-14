using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Common.Mail
{
    public interface IMailManager
    {
        void SendEmail(IList<string> recipients, string subject, string content);
        void SendEmail(Dictionary<string, Object> vars, string subject, string content);
        void SendTemplateEmail(Dictionary<string,object> vars, string subject, string templateName, object dataObject);
    }
}
