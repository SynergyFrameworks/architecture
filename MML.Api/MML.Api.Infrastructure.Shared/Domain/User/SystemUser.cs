using System;
using System.Collections.Generic;
using System.Text;

namespace MML.Messenger.Core.Domain.User
{
   public class SystemUser
    {

        public virtual string Id { get; set; }
        public virtual string Username { get; set; }
       
        public virtual string Password { get; set; }
        public virtual string Email { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
       
        public virtual bool IsSystemUser { get; set; }
        public virtual string Role { get; set; }
        public virtual DateTime? LastLoggedInDate { get; set; }
       
        public virtual string Picture
        {
            get;
            set;
        }

        public virtual Tenant Tenant { get; set; }

        public SystemUser() { }

        
    }
}

