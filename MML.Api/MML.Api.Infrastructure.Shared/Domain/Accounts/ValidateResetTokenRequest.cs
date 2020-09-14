using System.ComponentModel.DataAnnotations;

namespace MML.Messenger.Core.Domain.Accounts
{
    public class ValidateResetTokenRequest
    {
        [Required]
        public string Token { get; set; }
    }
}