using FriendsCoreAPI;
using FriendsCoreAPI.Models;
using FriendsCoreAPI.Hubs;
using FriendsCoreAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FriendsCoreAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<ProfileHub> _profileHub;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            AppDbContext context,
            IWebHostEnvironment env,
            IHubContext<ProfileHub> profileHub,
            ILogger<ProfileController> logger)
        {
            _context = context;
            _env = env;
            _profileHub = profileHub;
            _logger = logger;
        }

        [HttpGet("UserProfile")]
        public async Task<IActionResult> GetProfile(Guid UserId)
        {
            try
            {
                var profile = await _context.Users.FirstOrDefaultAsync(x=>x.Id == UserId);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile.");
                return StatusCode(500, "An unexpected error occurred while updating the profile.");
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromForm] string fullName, IFormFile? profileImage)
        {
            try
            {
                var userId = User.GetUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                    return NotFound("User not found.");

                user.FullName = fullName;

                if (profileImage != null && profileImage.Length > 0)
                {
                    var ext = Path.GetExtension(profileImage.FileName);
                    var filename = $"{Guid.NewGuid()}{ext}";
                    var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, filename);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }

                    user.ProfileImageUrl = $"/uploads/{filename}";
                }

                await _context.SaveChangesAsync();

                await ProfileHub.BroadcastProfileUpdate(_profileHub, user.Id.ToString(), new
                {
                    userId = user.Id,
                    fullName = user.FullName,
                    profileImageUrl = user.ProfileImageUrl
                });

                return Ok(new
                {
                    user.Id,
                    user.FullName,
                    user.ProfileImageUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile.");
                return StatusCode(500, "An unexpected error occurred while updating the profile.");
            }
        }
    }
}
