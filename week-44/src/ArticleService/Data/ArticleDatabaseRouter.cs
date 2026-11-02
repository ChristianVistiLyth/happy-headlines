using ArticleService.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

            // We don't use Kerberos login; without this the driver probes for it and logs an error
            var connection = new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable };

            _optionsByRegion[region] = new DbContextOptionsBuilder<ArticleDbContext>()
                .UseNpgsql(connection.ConnectionString)
                .UseAsyncSeeding((db, _, cancellationToken) => SampleArticles.SeedAsync(db, region, cancellationToken))
                .Options;
        }
    }

    public ArticleDbContext Open(Region region) => new(_optionsByRegion[region]);

    /// <summary> Creates or updates the tables in all eight databases and adds the sample articles.</summary>
    public async Task MigrateAllAsync()
    {
        foreach (var region in Enum.GetValues<Region>())
        {
            await using var db = Open(region);
            await db.Database.MigrateAsync();
        }
    }
}
