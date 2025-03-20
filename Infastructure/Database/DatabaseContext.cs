using Application.Infastructure.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Infastructure.Database;

public class DatabaseContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<UserDo> Users => Set<UserDo>();
    
    public DbSet<PostDo> Posts => Set<PostDo>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostDo>()
            .HasOne(post => post.CreatedByUser)
            .WithMany(user => user.Posts)
            .HasForeignKey(post => post.CreatedBy)
            .IsRequired();
    }
}