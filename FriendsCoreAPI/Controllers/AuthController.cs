using FriendsCoreAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FriendsCoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext db, ILogger<AuthController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Register([FromForm] RegisterViewModel dto)
        {
            try
            {
                if (dto == null ||
                    string.IsNullOrWhiteSpace(dto.Email) ||
                    string.IsNullOrWhiteSpace(dto.Password) ||
                    string.IsNullOrWhiteSpace(dto.FullName))
                {
                    return BadRequest(new { message = "Invalid input." });
                }

                var exists = await _db.Users.AnyAsync(x => x.Email == dto.Email);
                if (exists)
                {
                    return Conflict(new { message = "User with this email already exists." });
                }

                var user = new AppUser
                {
                    Email = dto.Email,
                    Password = dto.Password, // ✅ Consider hashing in production
                    FullName = dto.FullName,
                    ProfileImageUrl = dto.ProfileImagePath // ✅ Should be validated on upload
                };

                await _db.Users.AddAsync(user);
                await _db.SaveChangesAsync();

                return Ok(new { message = "User registered successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during registration.");
                return StatusCode(500, new { message = "An error occurred while registering user." });
            }
        }
    }
}
