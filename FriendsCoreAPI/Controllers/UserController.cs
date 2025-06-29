using FriendsCoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FriendsCoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(AppDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("Get")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound();

                return Ok(new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    ProfileImageUrl = user.ProfileImageUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user with ID: {id}");
                return StatusCode(500, "An unexpected error occurred while fetching user data.");
            }
        }
    }
}
