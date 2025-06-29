using FriendsCoreAPI.Models;
using FriendsCoreAPI;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class PostHub : Hub
{
    private readonly AppDbContext _context;

    public PostHub(AppDbContext context)
    {
        _context = context;
    }

    public async Task NotifyLike(Guid postId)
    {
        var userId = Context.UserIdentifier;
        if (Guid.TryParse(userId, out Guid parsedUserId))
        {
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == parsedUserId);

            if (existingLike == null)
            {
                _context.Likes.Add(new Like
                {
                    PostId = postId,
                    UserId = parsedUserId,
                    Timestamp = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        // 🔄 Broadcast updated like to all
        await Clients.All.SendAsync("ReceiveLike", postId);
    }

    public async Task NotifyComment(Guid postId, string authorName, string text)
    {
        var userId = Context.UserIdentifier;
        if (Guid.TryParse(userId, out Guid parsedUserId) && !string.IsNullOrWhiteSpace(text))
        {
            var comment = new Comment
            {
                PostId = postId,
                UserId = parsedUserId,
                Text = text,
                Timestamp = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // 🔄 Broadcast the comment to all users
            await Clients.All.SendAsync("ReceiveComment", new
            {
                PostId = postId,
                AuthorName = authorName,
                Text = comment.Text,
                Timestamp = comment.Timestamp.ToLocalTime()
            });
        }
    }
}
