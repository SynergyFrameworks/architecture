using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mandrill;
using Mandrill.Models;
using RestSharp;

namespace MML.Enterprise.Common.Mail
{
    public class MandrillMailManager:AbstractMailManager
    {
        private string ApiKey { get; set; }
        private string FromAddress { get; set; }
        private string FromName { get; set; }
        public void SendEmail(IList<string> recipients, string subject, string content)
        {
            var api = new MandrillApi(ApiKey);

            //var result = api.SendMessage(recipients.Select(r => new EmailAddress(r)), subject, content,
            //    new EmailAddress(FromAddress, FromName));
        }
    }
}
