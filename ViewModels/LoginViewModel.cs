using System.ComponentModel.DataAnnotations;

namespace GPMS.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public required string Username { get; set; }

        [Required]
        public required string Password { get; set; }

        [Required]
        public required string Captcha { get; set; }

        public string? CaptchaCode { get; set; }
    }
}
