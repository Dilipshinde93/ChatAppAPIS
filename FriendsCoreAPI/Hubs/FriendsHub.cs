using Microsoft.AspNetCore.SignalR;

namespace FriendsCoreAPI.Hubs
{
    public class FriendsHub : Hub
    {
        public async Task NotifyRequestSent(Guid toUserId)
        {
            await Clients.All.SendAsync("FriendRequestReceived", toUserId);
        }

        public async Task NotifyRequestAccepted(Guid userId)
        {
            await Clients.All.SendAsync("FriendRequestAccepted", userId);
        }
    }
}
