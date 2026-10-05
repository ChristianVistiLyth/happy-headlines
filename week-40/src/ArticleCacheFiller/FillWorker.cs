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
        // TODO (you): the loop - fill, wait, fill again ...
        throw new NotImplementedException();
    }

    /// <summary>One fill: reads the Global articles of the last 14 days and writes each one to Redis. Returns how many.</summary>
    private async Task<int> FillAsync(CancellationToken cancellationToken)
    {
        // TODO (you)
        throw new NotImplementedException();
    }

    // The same key ArticleService reads: "article:{id}"
    private static string Key(Guid id) => $"article:{id}";
}
