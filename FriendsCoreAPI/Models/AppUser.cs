namespace FriendsCoreAPI.Models
{
    public class AppUser
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; }= string.Empty;
        public string? ProfileImageUrl { get; set; }

        public ICollection<FriendRequest>? SentRequests { get; set; }
        public ICollection<FriendRequest>? ReceivedRequests { get; set; }
    }

    public class FriendRequest
    {
        public Guid Id { get; set; }

        public Guid FromUserId { get; set; }
        public AppUser FromUser { get; set; }

        public Guid ToUserId { get; set; }
        public AppUser ToUser { get; set; } 

        public bool IsAccepted { get; set; } = false;
    }
}
