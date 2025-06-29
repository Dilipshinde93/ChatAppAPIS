using FriendsCoreAPI.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/presence")]
public class PresenceController : ControllerBase
{
    private readonly ILogger<PresenceController> _logger;

    public PresenceController(ILogger<PresenceController> logger)
    {
        _logger = logger;
    }

    [HttpGet("online")]
    public IActionResult GetOnlineUsers()
    {
        try
        {
            var users = PresenceHub.GetOnlineUsers();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving online users.");
            return StatusCode(500, "Failed to retrieve online users.");
        }
    }
}
