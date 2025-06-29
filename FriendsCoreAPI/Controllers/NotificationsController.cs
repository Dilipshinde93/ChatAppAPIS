using FriendsCoreAPI.Extensions;
using FriendsCoreAPI.Hubs;
using FriendsCoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FriendsCoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationsHub> _hub;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(AppDbContext context, IHubContext<NotificationsHub> hub, ILogger<NotificationsController> logger)
        {
            _context = context;
            _hub = hub;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Notification notification)
        {
            try
            {
                notification.Timestamp = DateTime.UtcNow;
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                await NotificationsHub.SendToUser(_hub, notification.UserId.ToString(), notification);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification.");
                return StatusCode(500, "An error occurred while creating the notification.");
            }
        }

        [HttpGet("unread")]
        public IActionResult GetUnread()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var unread = _context.Notifications
                    .Where(n => n.UserId.ToString() == userId && !n.IsRead)
                    .OrderByDescending(n => n.Timestamp)
                    .ToList();

                return Ok(unread);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch unread notifications.");
                return StatusCode(500, "Unable to get unread notifications.");
            }
        }

        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var unread = _context.Notifications
                    .Where(n => n.UserId.ToString() == userId && !n.IsRead)
                    .ToList();

                foreach (var note in unread)
                    note.IsRead = true;

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notifications as read.");
                return StatusCode(500, "Failed to mark as read.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var count = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get unread count.");
                return StatusCode(500, "Could not get count.");
            }
        }

        [HttpGet("all")]
        public IActionResult GetAllNotifications()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var all = _context.Notifications
                    .Where(n => n.UserId.ToString() == userId)
                    .OrderByDescending(n => n.Timestamp)
                    .ToList();

                return Ok(all);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all notifications.");
                return StatusCode(500, "Error loading notifications.");
            }
        }
    }
}
