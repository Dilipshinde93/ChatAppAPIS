using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FriendsCoreAPI.Hubs
{
    public class ProfileHub : Hub
    {
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

        // ✅ Called from client to update profile and notify others
        public async Task UpdateProfile(string fullName, string bio, string imageUrl)
        {
            var userId = Context.UserIdentifier;

            var payload = new
            {
                UserId = userId,
                FullName = fullName,
                Bio = bio,
                ProfileImageUrl = imageUrl
            };

            // Send update to all connections that have this user
            if (!string.IsNullOrEmpty(userId) && _connections.TryGetValue(userId, out var connId))
            {
                await Clients.Client(connId).SendAsync("ProfileUpdated", payload);
            }
        }

        // Optional: static helper if needed from elsewhere
        public static Task BroadcastProfileUpdate(IHubContext<ProfileHub> hub, string userId, object payload)
        {
            if (_connections.TryGetValue(userId, out var connId))
            {
                return hub.Clients.Client(connId).SendAsync("ProfileUpdated", payload);
            }
            return Task.CompletedTask;
        }
    }
}
