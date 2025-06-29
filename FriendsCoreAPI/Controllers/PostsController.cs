using FriendsCoreAPI;
using FriendsCoreAPI.Models;
using FriendsCoreAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using FriendsCoreAPI.Hubs;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<PostHub> _hub;
    private readonly IHubContext<StatsHub> _hubContextStats;
    private readonly IHubContext<NotificationsHub> _notifHub;
    private readonly ILogger<PostsController> _logger;

    public PostsController(
        AppDbContext context,
        IHubContext<PostHub> hub,
        IHubContext<StatsHub> hubContextStats,
        IHubContext<NotificationsHub> notifHub,
        ILogger<PostsController> logger)
    {
        _context = context;
        _hub = hub;
        _hubContextStats = hubContextStats;
        _notifHub = notifHub;
        _logger = logger;
    }

    [HttpGet("GetPosts")]
    public async Task<IActionResult> GetByUserId(Guid userId)
    {
        try
        {
            var posts = await _context.Posts
                .Where(p => p.AuthorId == userId)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.Timestamp)
                .ToListAsync();

            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving posts by user ID.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var posts = await _context.Posts
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.Timestamp)
                .ToListAsync();

            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all posts.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] PostCreateDto dto)
    {
        try
        {
            var userId = dto.UserId;
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            var post = new Post
            {
                Content = dto.Content,
                ImageUrl = dto.ImageUrl,
                AuthorId = user.Id,
                AuthorName = user.FullName,
                Timestamp = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("ReceivePost", post);
            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post.");
            return StatusCode(500, "Failed to create post.");
        }
    }

    [HttpPost("{postId}/like")]
    public async Task<IActionResult> LikePost(Guid postId)
    {
        try
        {
            var post = await _context.Posts
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return NotFound();

            var userName = User.Identity?.Name ?? "Anonymous";
            var userId = User.GetUserId();

            if (!post.Likes.Any(l => l.UserId == userId))
            {
                post.Likes.Add(new Like { PostId = postId, UserId = userId, UserName = userName });
                await _context.SaveChangesAsync();

                await _hub.Clients.All.SendAsync("ReceiveLike", postId, userName);

                if (post.AuthorId != userId)
                {
                    var notif = new Notification
                    {
                        UserId = post.AuthorId,
                        Content = $"{userName} liked your post"
                    };
                    _context.Notifications.Add(notif);
                    await _context.SaveChangesAsync();

                    await NotificationsHub.SendToUser(_notifHub, post.AuthorId.ToString(), notif);
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error liking post.");
            return StatusCode(500, "Failed to like post.");
        }
    }

    [HttpPost("{postId}/comments")]
    public async Task<IActionResult> AddComment(Guid postId, [FromForm] string text)
    {
        try
        {
            var post = await _context.Posts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return NotFound();

            var userName = User.Identity?.Name ?? "Anonymous";
            var userId = User.GetUserId();

            var comment = new Comment
            {
                PostId = postId,
                Text = text,
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("ReceiveComment", postId, comment);

            if (post.AuthorId != userId)
            {
                var notif = new Notification
                {
                    UserId = post.AuthorId,
                    Content = $"{userName} commented on your post: \"{text}\""
                };
                _context.Notifications.Add(notif);
                await _context.SaveChangesAsync();

                await NotificationsHub.SendToUser(_notifHub, post.AuthorId.ToString(), notif);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to post.");
            return StatusCode(500, "Failed to add comment.");
        }
    }
}
