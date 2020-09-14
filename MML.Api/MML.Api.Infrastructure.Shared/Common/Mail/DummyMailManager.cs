using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
namespace MML.Enterprise.Common.Mail
{
    public class DummyMailManager : IMailManager
    {
        private ILog Log = LogManager.GetLogger(typeof (DummyMailManager));
        private string ApiKey { get; set; }
        private string FromAddress { get; set; }
        private string FromName { get; set; }
        //mailgun 
        private Uri BaseUrl { get; set; }
        private string Api { get; set; }
        private string Domain { get; set; }
        private string Resource { get; set; }

        public void SendEmail(Dictionary<string, object> vars, string subject, string content)
        {
            Log.InfoFormat("Dummy sent email to  {0}. subject: {1}. Content:{2}", string.Join(" ", vars), subject, content);
        }

        public void SendEmail(IList<string> recipients, string subject, string content)
        {
            Log.InfoFormat("Dummy sent email to  {0}. subject: {1}. Content:{2}", string.Join(" ", recipients), subject, content);
        }

        public void SendTemplateEmail(Dictionary<string, object> vars, string subject, string templateName, object dataObject)
        {
            return;
        }
    }
}
