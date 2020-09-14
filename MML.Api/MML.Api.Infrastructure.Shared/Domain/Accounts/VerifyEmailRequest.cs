using System.ComponentModel.DataAnnotations;

namespace MML.Messenger.Core.Domain.Accounts
{ 
    public class VerifyEmailRequest
    {
        [Required]
        public string Token { get; set; }
    }
}