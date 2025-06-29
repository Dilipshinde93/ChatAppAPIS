using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using FriendsCoreAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FriendsCoreAPI.Hubs
{
    public class NotificationsHub : Hub
    {
        private readonly AppDbContext _context;

        public NotificationsHub(AppDbContext context)
        {
            _context = context;
        }

        private static readonly ConcurrentDictionary<string, string> _connections = new();

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

        public static Task SendToUser(IHubContext<NotificationsHub> hub, string userId, Notification notification)
        {
            if (_connections.TryGetValue(userId, out var connId))
            {
                return hub.Clients.Client(connId).SendAsync("ReceiveNotification", new
                {
                    notification.Id,
                    notification.Content,
                    notification.Type,
                    CreatedAt = notification.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    notification.IsRead
                });
            }

            return Task.CompletedTask;
        }
        public async Task<int> GetUnreadCount()
        {
            var userId = Context.UserIdentifier;

            if (Guid.TryParse(userId, out Guid uid))
            {
                var model = await _context.Notifications
                    .Where(n => n.UserId == uid && !n.IsRead)
                    .CountAsync();
                return model;
            }

            return 0;
        }
        public async Task MarkAllRead()
        {
            var userId = Context.UserIdentifier;
            if (Guid.TryParse(userId, out Guid parsedUserId))
            {
                var notifications = _context.Notifications
                    .Where(n => n.UserId == parsedUserId && !n.IsRead);

                foreach (var n in notifications)
                    n.IsRead = true;

                await _context.SaveChangesAsync();

                // Optionally notify the user
                await Clients.Caller.SendAsync("AllNotificationsRead");
            }
        }
    }
}
