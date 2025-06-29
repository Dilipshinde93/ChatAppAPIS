using FriendsCoreAPI.Models;
using FriendsCoreAPI;
using FriendsCoreAPI.Extensions;
using FriendsCoreAPI.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/friends")]
[Authorize]
public class FriendsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<FriendsHub> _hub;
    private readonly IHubContext<StatsHub> _hubStats;
    private readonly IHubContext<NotificationsHub> _notificationsHub;
    private readonly ILogger<FriendsController> _logger;

    public FriendsController(
        AppDbContext context,
        IHubContext<FriendsHub> hub,
        IHubContext<StatsHub> hubStats,
        IHubContext<NotificationsHub> notificationsHub,
        ILogger<FriendsController> logger)
    {
        _context = context;
        _hub = hub;
        _hubStats = hubStats;
        _notificationsHub = notificationsHub;
        _logger = logger;
    }

    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromForm] Guid toUserId, [FromForm] Guid fromUserId)
    {
        if (_context.FriendRequests.Any(fr => fr.FromUserId == fromUserId && fr.ToUserId == toUserId))
            return BadRequest("Already requested");

        try
        {
            _context.FriendRequests.Add(new FriendRequest
            {
                FromUserId = fromUserId,
                ToUserId = toUserId
            });

            await _context.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("FriendRequestReceived", toUserId);

            var sender = await _context.Users.FindAsync(fromUserId);
            if (sender != null)
            {
                var message = $"👥 You received a friend request from {sender.FullName}";
                var notification = new Notification
                {
                    UserId = toUserId,
                    Content = message,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                await NotificationsHub.SendToUser(_notificationsHub, toUserId.ToString(), notification);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending friend request.");
            return StatusCode(500, "Something went wrong.");
        }
    }

    [HttpPost("accept")]
    public async Task<IActionResult> Accept(Guid requestId)
    {
        var request = await _context.FriendRequests.FindAsync(requestId);
        if (request == null) return NotFound();

        try
        {
            request.IsAccepted = true;
            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("FriendRequestAccepted", request.FromUserId);
            await StatsHub.BroadcastUpdate(_hubStats, _context);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting friend request.");
            return StatusCode(500, "Failed to accept request.");
        }
    }

    [HttpPost("reject")]
    public async Task<IActionResult> Reject(Guid requestId)
    {
        var request = await _context.FriendRequests.FindAsync(requestId);
        if (request == null) return NotFound();

        try
        {
            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();
            await StatsHub.BroadcastUpdate(_hubStats, _context);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting friend request.");
            return StatusCode(500, "Failed to reject request.");
        }
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(Guid UserId)
    {
        try
        {
            var pending = await _context.FriendRequests
                .Include(r => r.FromUser)
                .Where(fr => fr.ToUserId == UserId && !fr.IsAccepted)
                .Select(fr => new FriendRequestDto
                {
                    Id = fr.Id,
                    FromUserId = fr.FromUserId,
                    Name = fr.FromUser.FullName,
                    ProfileImageUrl = fr.FromUser.ProfileImageUrl
                })
                .ToListAsync();

            return Ok(pending);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending requests.");
            return StatusCode(500, "Failed to retrieve pending requests.");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetFriends(Guid UserId)
    {
        try
        {
            var accepted = await _context.FriendRequests
                .Include(r => r.FromUser)
                .Include(r => r.ToUser)
                .Where(fr => fr.IsAccepted && (fr.FromUserId == UserId || fr.ToUserId == UserId))
                .ToListAsync();

            var friends = accepted
                .Select(fr => fr.FromUserId == UserId ? fr.ToUser : fr.FromUser)
                .Distinct()
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    ProfileImageUrl = u.ProfileImageUrl
                })
                .ToList();

            return Ok(friends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving friends list.");
            return StatusCode(500, "Failed to retrieve friends.");
        }
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(Guid UserId)
    {
        try
        {
            var allUsers = await _context.Users.ToListAsync();

            var friendRequests = await _context.FriendRequests
                .Where(fr => fr.FromUserId == UserId || fr.ToUserId == UserId)
                .ToListAsync();

            var friendIds = friendRequests
                .Where(fr => fr.IsAccepted)
                .Select(fr => fr.FromUserId == UserId ? fr.ToUserId : fr.FromUserId)
                .ToList();

            var pendingSentIds = friendRequests
                .Where(fr => !fr.IsAccepted && fr.FromUserId == UserId)
                .Select(fr => fr.ToUserId)
                .ToList();

            var suggestions = allUsers
                .Where(u => u.Id != UserId && !friendIds.Contains(u.Id))
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    ProfileImageUrl = u.ProfileImageUrl,
                    IsFriend = false,
                    RequestSent = pendingSentIds.Contains(u.Id)
                })
                .ToList();

            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching friend suggestions.");
            return StatusCode(500, "Failed to fetch suggestions.");
        }
    }
}
