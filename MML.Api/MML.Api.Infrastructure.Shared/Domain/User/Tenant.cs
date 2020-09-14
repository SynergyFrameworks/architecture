using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MML.Messenger.Core.Domain.User
{
    public class Tenant
    {

        public virtual string Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string BaseUrl { get; set; }
        public virtual string Logo { get; set; }
        public virtual string Settings { get; set; }
        public virtual string LogoPath { get; set; }
        public virtual string PdfLogoPath { get; set; }
        public virtual string LetterTemplatePath { get; set; }
        public virtual Dictionary<string, object> TenantSettings { get; set; }
     
        public virtual JObject GetApplicationSettings(string appName)
        {
            JObject return_value = null;
            if (TenantSettings.ContainsKey("Applications"))
            {
                var applications = (JArray)TenantSettings["Applications"];
                if (applications.Count(a => a["Id"].ToString() == appName) > 0)
                {
                    return_value = JObject.FromObject(applications.First(a => a["Id"].ToString() == appName));
                }
            }

            return return_value;
        }

    }
}

