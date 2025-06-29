using System;

namespace FriendsCoreAPI.Models
{
    public enum MessageStatus
    {
        Sent,
        Delivered,
        Read
    }

    public class ChatMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SenderId { get; set; } = Guid.Empty;
        public Guid ReceiverId { get; set; } = Guid.Empty;
        public string Message { get; set; } = string.Empty;
        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; } // "image", "file", etc.
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // 👇 New property replacing IsRead and IsDelivered
        public MessageStatus Status { get; set; } = MessageStatus.Sent;
    }
}
