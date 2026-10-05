using System.Text.Json;
using ArticleService.Models;
using StackExchange.Redis;

namespace ArticleService.Caching;

/// <summary>
/// The ArticleCache (Redis) in front of the Global article database.
/// The offline ArticleCacheFiller fills it; ArticleService only reads from it, and removes articles that change.
/// Every article is stored under the key "article:{id}" as JSON.
/// </summary>
public class ArticleCache(IConnectionMultiplexer redis, ILogger<ArticleCache> logger)
{
    /// <summary>
    /// Returns the article if it is in the cache (a hit), or null (a miss).
    /// If Redis is down, log a Warning and return null, so the caller reads the database instead.
    /// </summary>
    public async Task<Article?> TryGetAsync(Guid id)
    {
        try
        {
            var db = redis.GetDatabase();
            RedisValue json = await db.StringGetAsync(Key(id));

            // Not in the cache: a miss
            if (json.IsNull)
            {
                return null;
            }

            // In the cache: a hit - turn the JSON back into an article
            return JsonSerializer.Deserialize<Article>(json.ToString());
        }
        catch (Exception e) when (e is RedisException or RedisTimeoutException)
        {
            // The cache must never take ArticleService down: treat it as a miss and use the database
            logger.LogWarning("ArticleCache unavailable, reading article {ArticleId} from the database ({Reason})",
                id, e.GetType().Name);
            return null;
        }
    }

    /// <summary>
    /// Removes an article from the cache after it was updated or deleted in the database,
    /// so the next read gets the fresh version from the database.
    /// If Redis is down, log a Warning - the update or delete itself must not fail because of the cache.
    /// </summary>
    public async Task RemoveAsync(Guid id)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(Key(id));
        } 
        catch (Exception e) when (e is RedisException or RedisTimeoutException)
        {
        // The update or delete itself already succeeded already occured.
        logger.LogWarning("ArticleCache unavailable, article {ArticleId} not removed from the cache ({Reason})",
            id, e.GetType().Name);
        }
    }

    private static string Key(Guid id) => $"article:{id}";
}
