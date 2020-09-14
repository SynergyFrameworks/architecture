using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using log4net;
using System.IO;
using HandlebarsDotNet;

namespace MML.Enterprise.Common.Mail
{

    public class SmtpMailManager : AbstractMailManager
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SmtpMailManager));

        public int Port { get; set; }
        public string Host { get; set; }
        private string FromAddress { get; set; }
        private string FromDisplay { get; set; }
        public string Environment { get; set; }
        public bool EnableSsl { get; set; }

        public override void SendTemplateEmail(Dictionary<string, object> vars, string subject, string templateName, object dataObject)
        {
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
                SendEmail(vars, subject, processedTemplate, true);
                log.InfoFormat("{0} - Email Sent", dataObject);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("{0} - Error sending email {1}", dataObject, ex);
            }
        }
        public override void SendEmail(Dictionary<string, object> vars, string subject, string content)
        {
            SendEmail(vars, subject, content, false);
        }

        public void SendEmail(Dictionary<string, object> vars, string subject, string content, bool IsBodyHtml)
        {
            try
            {
                var mail = new MailMessage();

                ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
       
                var client = new SmtpClient
                {
                    Port = Port,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = true,
                    EnableSsl = EnableSsl,
                    Host = Host 
                };

                mail.Subject = string.Format("[{0}] {1}",Environment, subject);
                mail.Body = content;
                mail.From = new MailAddress(FromAddress, FromDisplay);  //Config
                mail.IsBodyHtml = IsBodyHtml;

                foreach (KeyValuePair<string, Object> entry in vars)
                {
                    mail.To.Add(entry.Key);
                }
                
                log.DebugFormat("Submitting email to mail server. Subject: {0}", subject);
                client.Send(mail);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Unable to send email. Exception {0} ", ex);
                throw;
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
