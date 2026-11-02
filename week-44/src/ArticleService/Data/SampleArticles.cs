using ArticleService.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ArticleService.Data;

/// <summary>Adds two sample articles to an empty region database, so there is something to show.</summary>
public static class SampleArticles
{
    private static readonly Dictionary<Region, string[]> Titles = new()
    {
        [Region.Africa] = ["Solar-powered schools light up rural villages", "Community gardens feed thousands"],
        [Region.Antarctica] = ["Penguin colony grows for the third year in a row", "Research station runs fully on wind power"],
        [Region.Asia] = ["Volunteers plant one million trees", "Library app teaches children to read"],
        [Region.Europe] = ["City swaps parking lots for parks", "Repair cafés keep electronics out of landfills"],
        [Region.NorthAmerica] = ["Neighbours build tiny homes for the homeless", "River cleanup brings the salmon back"],
        [Region.Oceania] = ["Coral reef shows signs of recovery", "Surf club teaches kids to swim for free"],
        [Region.SouthAmerica] = ["Rainforest replanting project doubles in size", "Street musicians fund a children's choir"],
        [Region.Global] = ["Global literacy rate reaches a new high", "Kindness challenge spreads to 100 countries"],
    };

    public static async Task SeedAsync(DbContext db, Region region, CancellationToken cancellationToken)
    {
        var articles = db.Set<Article>();
        if (await articles.AnyAsync(cancellationToken))
        {
            return;
        }

        var titles = Titles[region];
        for (var i = 0; i < titles.Length; i++)
        {
            articles.Add(new Article
            {
                Id = SampleId(region, i + 1),
                Title = titles[i],
                Content = "Sample article added when the database was created.",
                Author = "Happy Headlines",
                PublishedAt = DateTime.UtcNow.AddHours(-i),
                Region = region
            });
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The three instances start at the same time; another instance added the samples first
        }
    }

    /// <summary>Readable ids, e.g. Europe's first sample article is 00000004-0000-0000-0000-000000000001.</summary>
    public static Guid SampleId(Region region, int number) =>
        new($"{(int)region + 1:D8}-0000-0000-0000-{number:D12}");
}
