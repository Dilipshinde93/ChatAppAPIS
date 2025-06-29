using System.ComponentModel.DataAnnotations.Schema;

namespace FriendsCoreAPI.Models
{
    public class Post
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;
        public Guid AuthorId { get; set; }  // FK
        public AppUser Author { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public List<Like> Likes { get; set; }
        public List<Comment> Comments { get; set; }
    }

    public class Like
    {
        public Guid Id { get; set; }

        public Guid PostId { get; set; }
        public Post Post { get; set; }

        public Guid UserId { get; set; }
        public AppUser User { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string UserName { get; set; } = string.Empty;
    }

    public class Comment
    {
        public Guid Id { get; set; }

        public Guid PostId { get; set; }
        public Post Post { get; set; }

        public string Text { get; set; } = string.Empty;

        public Guid UserId { get; set; }
        public AppUser User { get; set; }

        public string UserName { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
