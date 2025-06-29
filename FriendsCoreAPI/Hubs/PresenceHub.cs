using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FriendsCoreAPI.Hubs
{
    public class PresenceHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _connections = new();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
                _connections[userId] = Context.ConnectionId;

            await Clients.All.SendAsync("UserOnline", userId);

            // Broadcast updated online user count to StatsHub
            await NotifyStatsUpdate();
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
                _connections.TryRemove(userId, out _);

            await Clients.All.SendAsync("UserOffline", userId);

            // Broadcast updated online user count to StatsHub
            await NotifyStatsUpdate();
            await base.OnDisconnectedAsync(exception);
        }

        public static List<string> GetOnlineUsers()
        {
            return _connections.Keys.ToList();
        }

        private async Task NotifyStatsUpdate()
        {
            var hubContext = Context.GetHttpContext()
                                     ?.RequestServices
                                     .GetService(typeof(IHubContext<StatsHub>)) as IHubContext<StatsHub>;

            if (hubContext != null)
            {
                await hubContext.Clients.All.SendAsync("ReceiveStatsUpdate", new
                {
                    onlineUsers = _connections.Count
                });
            }
        }
    }
}
