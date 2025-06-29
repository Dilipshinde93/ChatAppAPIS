namespace FriendsCoreAPI.Models
{
    public enum NotificationType
    {
        FriendRequest,
        Message,
        FriendSuggestion
    }

    public class Notification
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public Guid FromUserId { get; set; }
        public string Content { get; set; }
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
