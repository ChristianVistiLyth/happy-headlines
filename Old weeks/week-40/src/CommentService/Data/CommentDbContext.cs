using CommentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Data;

public class CommentDbContext(DbContextOptions<CommentDbContext> options) : DbContext(options)
{
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Comments are always looked up by article
        modelBuilder.Entity<Comment>().HasIndex(c => c.ArticleId);
    }
}
