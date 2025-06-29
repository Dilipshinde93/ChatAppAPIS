using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using FriendsCoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FriendsCoreAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationsHub> _notificationsHub;

        // Mapping between userId and their connectionId
        private static readonly ConcurrentDictionary<string, string> _connections = new();

        public ChatHub(AppDbContext context, IHubContext<NotificationsHub> notificationsHub)
        {
            _context = context;
            _notificationsHub = notificationsHub;
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
                _connections[userId] = Context.ConnectionId;

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
                _connections.TryRemove(userId, out _);

            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(Guid receiverId, string message, string senderName)
        {
            var senderId = new Guid(Context.UserIdentifier);

            var chat = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                Timestamp = DateTime.UtcNow,
                Status = MessageStatus.Sent
            };

            _context.ChatMessages.Add(chat);

            // Create notification
            var notification = new Notification
            {
                UserId = receiverId,
                Content = $"💬 New message from {senderName}",
                Timestamp = DateTime.UtcNow,
                IsRead = false,
                Type = NotificationType.Message,
                FromUserId = senderId
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            // Broadcast notification
            await _notificationsHub.Clients.User(receiverId.ToString())
                .SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    type = notification.Type.ToString(),
                    message = notification.Content,
                    createdAt = notification.Timestamp
                });

            var messagePayload = new
            {
                messageId = chat.Id,
                fromUser = senderId,
                toUser = receiverId,
                message = chat.Message,
                senderName,
                timestamp = chat.Timestamp,
                status = chat.Status.ToString()
            };

            // Send to receiver if online
            if (_connections.TryGetValue(senderId.ToString(), out var receiverConnectionId))
            {
                chat.Status = MessageStatus.Delivered;
                await _context.SaveChangesAsync();

                await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", messagePayload);
                await Clients.Caller.SendAsync("MessageDelivered", new { chat.Id });
            }
            else
            {
                await Clients.Caller.SendAsync("MessageSent", new { chat.Id });
            }
        }

        public async Task MarkAsDelivered(Guid messageId)
        {
            var message = await _context.ChatMessages.FirstOrDefaultAsync(m => m.Id == messageId);

            if (message != null && message.Status == MessageStatus.Sent)
            {
                message.Status = MessageStatus.Delivered;
                await _context.SaveChangesAsync();

                if (_connections.TryGetValue(message.SenderId.ToString(), out var senderConnection))
                {
                    await Clients.Client(senderConnection).SendAsync("MessageDelivered", new { messageId });
                }
            }
        }

        public async Task SendTyping(string receiverId, string senderName)
        {
            if (_connections.TryGetValue(receiverId, out string receiverConnection))
            {
                await Clients.Client(receiverConnection).SendAsync("UserTyping", senderName);
            }
        }
    }
}
