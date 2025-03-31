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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Post-User relationship
        modelBuilder.Entity<PostDo>()
            .HasOne(post => post.User) // Matches PostDo's navigation property
            .WithMany(user => user.Posts)
            .HasForeignKey(post => post.CreatedBy) // Matches PostDo's FK property
            .IsRequired();

        // Configure Comment-User relationship
        modelBuilder.Entity<CommentDo>()
            .HasOne(comment => comment.User)
            .WithMany(user => user.Comments)
            .HasForeignKey(comment => comment.UserId).OnDelete(DeleteBehavior.NoAction);

        // Configure Comment-Post relationship
        modelBuilder.Entity<CommentDo>()
            .HasOne(comment => comment.Post)
            .WithMany(post => post.Comments)
            .HasForeignKey(comment => comment.PostId)
            .IsRequired();

        // Configure Post-Channel relationship
        modelBuilder.Entity<PostDo>()
            .HasOne(post => post.Channel)
            .WithMany(channel => channel.Posts)
            .HasForeignKey(post => post.ChannelId);
        //.IsRequired(true);

        // Configure Channel-Community relationship
        modelBuilder.Entity<ChannelDo>()
            .HasOne(channel => channel.Community)
            .WithMany(community => community.Channels)
            .HasForeignKey(channel => channel.CommunityId)
            .IsRequired();

        // Configure many-to-many relationship: User <-> Community
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
    }
}