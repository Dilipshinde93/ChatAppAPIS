using FriendsCoreAPI;
using FriendsCoreAPI.Hubs;
using FriendsCoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/videos")]
[Authorize]
public class VideosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IHubContext<VideosHub> _hub;
    private readonly ILogger<VideosController> _logger;

    public VideosController(
        AppDbContext context,
        IWebHostEnvironment env,
        IHubContext<VideosHub> hub,
        ILogger<VideosController> logger)
    {
        _context = context;
        _env = env;
        _hub = hub;
        _logger = logger;
    }

    [HttpPost("upload")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string title)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No video file selected.");

        try
        {
            var uploads = Path.Combine(_env.WebRootPath, "videos");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var video = new Video
            {
                Title = title,
                FilePath = $"/videos/{fileName}",
                UploadedBy = Guid.Parse(userId!)
            };

            _context.Videos.Add(video);
            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("VideoUploaded", video);
            return Ok(video);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading video.");
            return StatusCode(500, "An error occurred while uploading the video.");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetVideos()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);

            var videos = await _context.Videos
                .Where(v => v.UploadedBy == userId)
                .OrderByDescending(v => v.UploadedAt)
                .ToListAsync();

            return Ok(videos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching video list.");
            return StatusCode(500, "Failed to retrieve video list.");
        }
    }
}
