using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FriendsCoreAPI.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FriendsCoreAPI.Controllers
{
    [Route("api/chat")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ChatController> _logger;

        public ChatController(AppDbContext context, ILogger<ChatController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages(Guid user1, Guid user2)
        {
            if (user1 == Guid.Empty || user2 == Guid.Empty)
            {
                _logger.LogWarning("Invalid user IDs provided to GetMessages.");
                return BadRequest("User IDs must not be empty.");
            }

            try
            {
                var messages = await _context.ChatMessages
                    .Where(m => (m.SenderId == user1 && m.ReceiverId == user2) ||
                                (m.SenderId == user2 && m.ReceiverId == user1))
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new
                    {
                        messageId = m.Id,
                        fromUser = m.SenderId,
                        toUser = m.ReceiverId,
                        message = m.Message,
                        timestamp = m.Timestamp,
                        status = m.Status.ToString(),
                        mediaUrl = m.MediaUrl,
                        mediaType = m.MediaType
                    })
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages between {User1} and {User2}", user1, user2);
                return StatusCode(500, "An error occurred while retrieving chat messages.");
            }
        }
    }
}
