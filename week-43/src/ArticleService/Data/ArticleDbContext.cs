using ArticleService.Models;
using Microsoft.EntityFrameworkCore;

namespace ArticleService.Data;

/// <summary>
/// One article database. All eight region databases share this schema;
/// <see cref="ArticleDatabaseRouter"/> decides which one a context talks to.
/// </summary>
public class ArticleDbContext(DbContextOptions<ArticleDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var article = modelBuilder.Entity<Article>();
        article.Property(a => a.Region).HasConversion<string>().HasMaxLength(20);
        article.HasIndex(a => a.PublishedAt);
    }
}
