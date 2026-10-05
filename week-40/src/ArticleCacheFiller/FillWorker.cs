using System.Text.Json;
using ArticleCacheFiller.Data;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using StackExchange.Redis;

namespace ArticleCacheFiller;

/// <summary>
/// The offline process that fills the ArticleCache: every few minutes it copies the Global articles of the
/// last 14 days from the database into Redis. No reader waits for it - it is not part of any request.
/// </summary>
public class FillWorker(IDbContextFactory<ArticleDbContext> dbFactory, IConnectionMultiplexer redis,
    IConfiguration configuration, ILogger<FillWorker> logger) : BackgroundService
{
    // How often the cache is filled (setting ArticleCacheFiller:Interval, default every 5 minutes)
    private readonly TimeSpan interval = TimeSpan.Parse(configuration["ArticleCacheFiller:Interval"] ?? "00:05:00");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Fill once right away, then again every interval
        while (!stoppingToken.IsCancellationRequested)
        {
            var wait = interval;
            try
            {
                // A timer has no incoming request, so nothing has started a trace yet. This span becomes the root,
                // and the database query and the Redis writes of this round hang under it: ONE trace per fill.
                using var activity = Tracing.Source.StartActivity("fill article cache");

                var articleCount = await FillAsync(stoppingToken);
                logger.LogInformation("ArticleCache filled with {ArticleCount} articles from the last 14 days", articleCount);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Database or Redis is down: try again soon. Meanwhile readers simply get misses (from the database).
                wait = TimeSpan.FromSeconds(30);
                logger.LogWarning("ArticleCache fill failed, trying again in {Seconds} seconds ({Reason})",
                    wait.TotalSeconds, e.GetType().Name);
            }

            await Task.Delay(wait, stoppingToken);
        }
    }

    /// <summary>One fill: reads the Global articles of the last 14 days and writes each one to Redis. Returns how many.</summary>
    private async Task<int> FillAsync(CancellationToken cancellationToken)
    {
        // The Global articles of the last 14 days
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var since = DateTime.UtcNow.AddDays(-14);
        var articles = await db.Articles.AsNoTracking()
            .Where(a => a.PublishedAt >= since)
            .ToListAsync(cancellationToken);

        var cache = redis.GetDatabase();
        foreach (var article in articles)
        {
            // Each article expires when it turns 14 days old: then Redis removes it by itself
            var expiresIn = article.PublishedAt.AddDays(14) - DateTime.UtcNow;
            await cache.StringSetAsync(Key(article.Id), JsonSerializer.Serialize(article), expiresIn);
        }

        return articles.Count;
    }

    // The same key ArticleService reads: "article:{id}"
    private static string Key(Guid id) => $"article:{id}";
}
