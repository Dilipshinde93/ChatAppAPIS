namespace FriendsCoreAPI.Models
{
    public class FriendRequestDto
    {
        public Guid Id { get; set; }
        public Guid FromUserId { get; set; }
        public string Name { get; set; } = "";
        public string? ProfileImageUrl { get; set; }
    }
}
