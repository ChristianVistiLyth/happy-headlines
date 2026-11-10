using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArticleService.Data;

/// <summary>
/// Only used by "dotnet ef migrations add". All region databases share one schema,
/// so the migrations are generated once and applied to every region at startup.
/// </summary>
public class DesignTimeArticleDbContextFactory : IDesignTimeDbContextFactory<ArticleDbContext>
{
    public ArticleDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ArticleDbContext>().UseNpgsql("Host=localhost;Database=articles").Options);
}
