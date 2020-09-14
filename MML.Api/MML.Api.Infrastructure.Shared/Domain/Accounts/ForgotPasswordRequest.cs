using System.ComponentModel.DataAnnotations;

namespace MML.Messenger.Core.Domain.Accounts
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}