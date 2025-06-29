using System.ComponentModel.DataAnnotations;

namespace FriendsCoreAPI.Models
{
    public class RegisterViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? FullName { get; set; }
        public string ProfileImagePath { get; set; } = string.Empty;

        public IFormFile? ProfileImage { get; set; }
    }
}
