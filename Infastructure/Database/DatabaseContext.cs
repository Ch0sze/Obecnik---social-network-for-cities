using Application.Infastructure.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Infastructure.Database;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<UserDo> Users => Set<UserDo>();
    public DbSet<CommunityDo> Communities => Set<CommunityDo>();
    public DbSet<ChannelDo> Channels => Set<ChannelDo>();
    public DbSet<UserCommunityDo> UserCommunities => Set<UserCommunityDo>();
    public DbSet<PostDo> Posts => Set<PostDo>();
    public DbSet<CommentDo> Comments => Set<CommentDo>();
    public DbSet<ApiLoginDo> ApiLogins => Set<ApiLoginDo>();
    public DbSet<AdminRequestDo> AdminRequests => Set<AdminRequestDo>(); // New DbSet

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Post -> User
        modelBuilder.Entity<PostDo>()
            .HasOne(post => post.User)
            .WithMany(user => user.Posts)
            .HasForeignKey(post => post.CreatedBy)
            .IsRequired();

        // Comment -> User
        modelBuilder.Entity<CommentDo>()
            .HasOne(comment => comment.User)
            .WithMany(user => user.Comments)
            .HasForeignKey(comment => comment.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Comment -> Post
        modelBuilder.Entity<CommentDo>()
            .HasOne(comment => comment.Post)
            .WithMany(post => post.Comments)
            .HasForeignKey(comment => comment.PostId)
            .IsRequired();

        // Post -> Channel
        modelBuilder.Entity<PostDo>()
            .HasOne(post => post.Channel)
            .WithMany(channel => channel.Posts)
            .HasForeignKey(post => post.ChannelId);

        // Channel -> Community
        modelBuilder.Entity<ChannelDo>()
            .HasOne(channel => channel.Community)
            .WithMany(community => community.Channels)
            .HasForeignKey(channel => channel.CommunityId)
            .IsRequired();

        // User <-> Community (many-to-many)
        modelBuilder.Entity<UserCommunityDo>()
            .HasKey(uc => new { uc.UserId, uc.CommunityId });

        modelBuilder.Entity<UserCommunityDo>()
            .HasOne(uc => uc.User)
            .WithMany(user => user.UserCommunities)
            .HasForeignKey(uc => uc.UserId)
            .IsRequired();

        modelBuilder.Entity<UserCommunityDo>()
            .HasOne(uc => uc.Community)
            .WithMany(community => community.UserCommunities)
            .HasForeignKey(uc => uc.CommunityId)
            .IsRequired();

        // AdminRequest -> User
        modelBuilder.Entity<AdminRequestDo>()
            .HasOne(ar => ar.User)
            .WithMany() // Add .WithMany(user => user.AdminRequests) if reverse nav is needed
            .HasForeignKey(ar => ar.UserId)
            .IsRequired();

        // AdminRequest -> Community
        modelBuilder.Entity<AdminRequestDo>()
            .HasOne(ar => ar.Community)
            .WithMany() // Add .WithMany(community => community.AdminRequests) if reverse nav is needed
            .HasForeignKey(ar => ar.CommunityId)
            .IsRequired();
    }
}
