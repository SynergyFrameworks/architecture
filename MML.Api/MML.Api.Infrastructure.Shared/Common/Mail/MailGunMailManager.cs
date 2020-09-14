using System;
using System.Collections.Generic;
using RestSharp;
using System.IO;
using log4net;
using HandlebarsDotNet;
using RestSharp.Authenticators;

namespace MML.Enterprise.Common.Mail
{
    public class MailGunMailManager :AbstractMailManager
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MailGunMailManager));

        private Uri BaseUrl { get; set; }
        private string Api { get; set; }
        private string Domain { get; set; }
        private string Resource { get; set; }
        private string FromAddress { get; set; }

        public override void SendEmail(Dictionary<string,object> vars, string subject, string content)
        {
            RestClient client = new RestClient();
            client.BaseUrl = BaseUrl;
            client.Authenticator = new HttpBasicAuthenticator("api",Api);
            RestRequest request = new RestRequest();
            request.AddParameter("domain", Domain, ParameterType.UrlSegment);
            request.Resource = Resource;
            request.AddParameter("from", FromAddress);
            foreach (KeyValuePair<string, Object> entry in vars)
            {
                request.AddParameter("to", entry.Key);
            }
            request.AddParameter("subject", subject);
            request.AddParameter("html", content);
            request.Method = Method.POST;
            client.Execute(request);
        }

        public override void SendTemplateEmail(Dictionary<string,object> vars, string subject, string templateName, object dataObject) {
            // Should take info the fill the template as the parameter


            var emailTemplate = GetEmailTemplate(templateName);

            if (emailTemplate == null)
            {
                emailTemplate = "Template Not Found";
            }

            var template = Handlebars.Compile(emailTemplate);

            var processedTemplate = template(dataObject);

            try
            {
                log.InfoFormat("{0} - Sending Email", dataObject);
                SendEmail(vars, subject, processedTemplate);
                log.InfoFormat("{0} - Email Sent", dataObject);
            }
            catch (Exception ex)
            {
               log.ErrorFormat("{0} - Error sending email {1}", dataObject, ex);
            }
        }

        private string GetEmailTemplate(string templateName)
        {
            var templatePath = string.Format(AppDomain.CurrentDomain.BaseDirectory + "{0}", templateName);
            using (var stream = new StreamReader(templatePath))
            {
                string result = stream.ReadToEnd();
                return result;
            }
        }

    }
}
