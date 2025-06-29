namespace FriendsCoreAPI.Models
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsFriend { get; set; }             // unused for suggestions but okay
        public bool RequestSent { get; set; }          // should be false for suggestions
        public string? ProfileImageUrl { get; set; }
    }
}
