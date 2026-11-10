using System.Text.Json;
using CommentService.Models;
using StackExchange.Redis;

namespace CommentService.Caching;

/// <summary>
/// The CommentCache (Redis) in front of the comment database, filled on a cache miss.
/// It holds the comments of the most recently read articles (at most CommentCache:MaxArticles, 30).
/// When it is full, the least recently used article is removed (LRU). Two kinds of keys in Redis:
///   "comments:{articleId}"  the comments of one article, as JSON
///   "lru:comments"          a ranking (sorted set) of article ids by the time they were last read
/// </summary>
public class CommentCache(IConnectionMultiplexer redis, IConfiguration configuration, ILogger<CommentCache> logger)
{
    private const string LruKey = "lru:comments";
    private readonly int maxArticles = int.Parse(configuration["CommentCache:MaxArticles"] ?? "30");

    /// <summary>
    /// Returns the article's comments if they are in the cache (a hit) and marks the article as just used,
    /// or null (a miss). If Redis is down, log a Warning and return null, so the caller reads the database.
    /// </summary>
    public async Task<List<Comment>?> TryGetAsync(Guid articleId)
    {
        try
        {
            var db = redis.GetDatabase();
            RedisValue json = await db.StringGetAsync(Key(articleId));

            // Not in the cache: a miss
            if (json.IsNull)
            {
                return null;
            }

            // A hit: the article was just used, so it moves to the top of the ranking (score = now)
            await db.SortedSetAddAsync(LruKey, articleId.ToString(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return JsonSerializer.Deserialize<List<Comment>>(json.ToString());
        }
        catch (Exception e) when (e is RedisException or RedisTimeoutException)
        {
            // The cache must never take CommentService down: treat it as a miss and use the database
            logger.LogWarning("CommentCache unavailable, reading comments of article {ArticleId} from the database ({Reason})",
                articleId, e.GetType().Name);
            return null;
        }
    }

    /// <summary>
    /// After a miss: stores the comments that were read from the database, marks the article as just used,
    /// and - while more than maxArticles articles are cached - removes the least recently used one (LRU).
    /// If Redis is down, log a Warning: the reader already has the comments from the database.
    /// </summary>
    public async Task AddAsync(Guid articleId, List<Comment> comments)
    {
        try
        {
            var db = redis.GetDatabase();

            // Store the comments, and put the article at the top of the ranking: its score is "now"
            await db.StringSetAsync(Key(articleId), JsonSerializer.Serialize(comments));
            await db.SortedSetAddAsync(LruKey, articleId.ToString(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            // LRU: while the cache holds too many articles, remove the one that was used longest ago
            while (await db.SortedSetLengthAsync(LruKey) > maxArticles)
            {
                // Pop the member with the lowest score = the least recently used article
                var oldest = await db.SortedSetPopAsync(LruKey, Order.Ascending);
                if (oldest is null)
                {
                    break;
                }

                var oldestArticleId = Guid.Parse(oldest.Value.Element.ToString());
                await db.KeyDeleteAsync(Key(oldestArticleId));
                logger.LogInformation("CommentCache full: comments of article {ArticleId} removed (least recently used)",
                    oldestArticleId);
            }
        }
        catch (Exception e) when (e is RedisException or RedisTimeoutException)
        {
            // The reader already has the comments from the database; they are just not cached this time
            logger.LogWarning("CommentCache unavailable, comments of article {ArticleId} not cached ({Reason})",
                articleId, e.GetType().Name);
        }
    }

    /// <summary>
    /// After a new comment: removes the article's comments from the cache, so the next read is a miss
    /// and loads them - including the new one - from the database. If Redis is down, log a Warning.
    /// </summary>
    public async Task RemoveAsync(Guid articleId)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(Key(articleId));                       // the comments
            await db.SortedSetRemoveAsync(LruKey, articleId.ToString());   // and the article's place in the ranking
        }
        catch (Exception e) when (e is RedisException or RedisTimeoutException)
        {
            // The comment is already saved in the database - don't fail the request because of the cache
            logger.LogWarning("CommentCache unavailable, comments of article {ArticleId} not removed from the cache ({Reason})",
                articleId, e.GetType().Name);
        }
    }

    /// <summary>How many articles' comments are cached right now (for the dashboard), or null if Redis is down.</summary>
    public long? CountArticles()
    {
        try
        {
            return redis.GetDatabase().SortedSetLength(LruKey);
        }
        catch (Exception e) when (e is RedisException or RedisTimeoutException)
        {
            return null;
        }
    }

    private static string Key(Guid articleId) => $"comments:{articleId}";
}
