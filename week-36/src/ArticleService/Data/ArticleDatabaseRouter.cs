using ArticleService.Models;
using Microsoft.EntityFrameworkCore;

namespace ArticleService.Data;

/// <summary>
/// The z-axis split: routes every request to the database of its region.
/// Connection strings come from configuration, e.g. ArticleDatabases:Europe.
/// </summary>
public class ArticleDatabaseRouter
{
    private readonly Dictionary<Region, DbContextOptions<ArticleDbContext>> _optionsByRegion = new();

    public ArticleDatabaseRouter(IConfiguration configuration)
    {
        foreach (var region in Enum.GetValues<Region>())
        {
            // Fail at startup if a region has no database configured
            var connectionString = configuration[$"ArticleDatabases:{region}"]
                ?? throw new InvalidOperationException($"Missing connection string 'ArticleDatabases:{region}'");

            _optionsByRegion[region] = new DbContextOptionsBuilder<ArticleDbContext>()
                .UseNpgsql(connectionString)
                .UseAsyncSeeding((db, _, cancellationToken) => SampleArticles.SeedAsync(db, region, cancellationToken))
                .Options;
        }
    }

    public ArticleDbContext Open(Region region) => new(_optionsByRegion[region]);

    /// <summary>Creates or updates the tables in all eight databases and adds the sample articles.</summary>
    public async Task MigrateAllAsync()
    {
        foreach (var region in Enum.GetValues<Region>())
        {
            await using var db = Open(region);
            await db.Database.MigrateAsync();
        }
    }
}
