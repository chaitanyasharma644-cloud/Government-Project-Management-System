using System.ComponentModel.DataAnnotations;

namespace GPMS.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        public string EmailOrUsername { get; set; } = string.Empty;
    }
}