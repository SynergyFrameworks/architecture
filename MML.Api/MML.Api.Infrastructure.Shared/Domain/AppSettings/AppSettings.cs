using System;
using System.Collections.Generic;
using System.Text;

namespace MML.Messenger.Core.Domain.AppSettings
{
    public class AppSettings
    {
        public string AppPrefixPath { get; set; }
        public string JwtSecretKey { get; set; }
        public string WebApiUrl { get; set; }
        public string[] AllowedOrigins { get; set; }
    }

}
