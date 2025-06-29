using FriendsCoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FriendsCoreAPI
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // FriendRequests
            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.FromUser)
                .WithMany(u => u.SentRequests)
                .HasForeignKey(fr => fr.FromUserId)
                .OnDelete(DeleteBehavior.NoAction);  // ✅ prevent cascade path

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.ToUser)
                .WithMany(u => u.ReceivedRequests)
                .HasForeignKey(fr => fr.ToUserId)
                .OnDelete(DeleteBehavior.NoAction);  // ✅ prevent cascade path

            // Posts
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict); // ✅ disable cascade

            // Likes
            modelBuilder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict); // ✅ disable cascade

            // Comments
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict); // ✅ disable cascade
        }
    }
}
