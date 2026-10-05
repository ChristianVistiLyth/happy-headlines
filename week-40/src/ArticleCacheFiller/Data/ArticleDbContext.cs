using ArticleCacheFiller.Models;
using Microsoft.EntityFrameworkCore;

namespace ArticleCacheFiller.Data;

/// <summary>Read-only view of the Global article database (ArticleService creates and owns the table).</summary>
public class ArticleDbContext(DbContextOptions<ArticleDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
}
