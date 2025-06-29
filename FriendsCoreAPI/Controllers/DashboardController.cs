using FriendsCoreAPI.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace FriendsCoreAPI.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(AppDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            try
            {
                var totalPosts = _context.Posts.Count();
                var pendingRequests = _context.FriendRequests.Count(fr => !fr.IsAccepted);
                var onlineCount = PresenceHub.GetOnlineUsers().Count;

                return Ok(new
                {
                    totalPosts,
                    pendingRequests,
                    onlineUsers = onlineCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch dashboard statistics.");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "An error occurred while retrieving dashboard data."
                });
            }
        }
    }
}
