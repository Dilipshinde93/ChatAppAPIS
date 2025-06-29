using FriendsCoreAPI;
using FriendsCoreAPI.Hubs;
using FriendsCoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Route("api/messages")]
[ApiController]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<MessagesController> _logger;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessagesController(AppDbContext context, ILogger<MessagesController> logger, IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    // ✅ Get all messages between current user and a given user
    [HttpGet("GetMessages")]
    public async Task<IActionResult> GetMessages(Guid otherUserId)
    {
        try
        {
            var userId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (userId == Guid.Empty) return Unauthorized();

            var messages = await _context.ChatMessages
                .Where(m =>
                    (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                    (m.ReceiverId == userId && m.SenderId == otherUserId))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve messages.");
            return StatusCode(500, new { error = "An error occurred while fetching messages." });
        }
    }

    // ✅ Media message endpoint
    [HttpPost("send-media")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> SendMedia([FromForm] IFormFile file, [FromForm] Guid receiverId)
    {
        try
        {
            var senderId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (senderId == Guid.Empty || file == null)
                return BadRequest("Missing sender or file.");

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var mediaType = file.ContentType.StartsWith("image") ? "image" : "file";

            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                MediaUrl = $"/uploads/{fileName}",
                MediaType = mediaType,
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", new
            {
                fromUser = senderId,
                mediaUrl = message.MediaUrl,
                mediaType = mediaType,
                timestamp = message.Timestamp
            });

            return Ok(new { message = "Media sent successfully", url = message.MediaUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send media message.");
            return StatusCode(500, new { error = "An error occurred while sending media." });
        }
    }
}
