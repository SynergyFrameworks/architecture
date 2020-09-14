using System;
using System.Collections.Generic;
using System.Text;

namespace MML.Messenger.Core.Domain.Calling
{
    public class CallerConnection
    {
        public WebRTCUser Caller { get; set; }
        public WebRTCUser Callee { get; set; }
    }

    public class WebRTCUser
    {
        public string Username { get; set; }
        public string ConnectionId { get; set; }
        public bool InCall { get; set; }
    }

    public class UserCall
    {
        public List<WebRTCUser> Users { get; set; }
    }
}

    

