using Microsoft.AspNetCore.SignalR;

namespace FriendsCoreAPI.Hubs
{
    public class StatsHub : Hub
    {
        public static async Task BroadcastUpdate(IHubContext<StatsHub> hub, AppDbContext context)
        {
            var stats = new
            {
                totalPosts = context.Posts.Count(),
                pendingRequests = context.FriendRequests.Count(fr => !fr.IsAccepted),
                onlineUsers = PresenceHub.GetOnlineUsers().Count
            };

            await hub.Clients.All.SendAsync("ReceiveStatsUpdate", stats);
        }
    }
}
