using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using ArticleService.Caching;
using ArticleService.Data;
using ArticleService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monitoring;

namespace ArticleService.Controllers;

/// <summary>
/// CRUD for articles. The region in the URL decides which of the eight databases is used.
/// Logging policy: Information when something changes, Warning when an article is not found, ids only.
/// </summary>
[ApiController]
[Route("api/regions/{region:region}/articles")]
public class ArticlesController(ArticleDatabaseRouter router, ArticleCache cache, ILogger<ArticlesController> logger) : ControllerBase
{
    // Two ways to ask for a list, both so that only what is needed travels over the network:
    //   ?since=<time>          GAF "Reduce Network Package Size": every article published after that time
    //                          (the daily newsletter asks for the last 24 hours; the index on PublishedAt finds them)
    //   ?page=1&pageSize=10    GAF "Defer Fetching": one page of the newest articles; the next page is only
    //                          fetched if someone asks for it. Without parameters: the 10 newest, as on the website.
    [HttpGet]
    public async Task<ActionResult<List<Article>>> GetAll(Region region, DateTime? since,
        [Range(1, int.MaxValue)] int page = 1, [Range(1, MaxPageSize)] int pageSize = DefaultPageSize)
    {
        await using var db = router.Open(region);
        var articles = db.Articles.AsNoTracking().OrderByDescending(a => a.PublishedAt);

        if (since is not null)
        {
            // A time window is already limited by time, so it isn't cut into pages
            return await articles.Where(a => a.PublishedAt > since.Value.ToUniversalTime()).ToListAsync();
        }

        return await articles.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    private const int DefaultPageSize = 10;     // the website shows the ten most recent articles
    private const int MaxPageSize = 50;         // a bigger page is answered with 400 Bad Request

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Article>> Get(Region region, Guid id)
    {
        var timer = Stopwatch.StartNew();   // how long the read takes, for the dashboard

        if (region == Region.Global)
        {
            var cached = await cache.TryGetAsync(id);
            if (cached is not null)
            {
                CacheMetrics.Record("article", hit: true, timer.Elapsed);
                return cached;
            }
        }

        await using var db = router.Open(region);
        var article = await db.Articles.FindAsync(id);
        if (region == Region.Global)
        {
            CacheMetrics.Record("article", hit: false, timer.Elapsed);   // a miss: the trip to the database is included
        }

        if (article is null)
        {
            logger.LogWarning("Article {ArticleId} not found in {Region}", id, region);
            return NotFound();
        }

        return article;
    }

    [HttpPost]
    public async Task<ActionResult<Article>> Create(Region region, ArticleRequest request)
    {
        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            Author = request.Author,
            PublishedAt = DateTime.UtcNow,
            Region = region
        };

        await using var db = router.Open(region);
        db.Articles.Add(article);
        await db.SaveChangesAsync();
        logger.LogInformation("Article {ArticleId} created in {Region} by {Author}", article.Id, region, article.Author);

        return CreatedAtAction(nameof(Get), new { region, id = article.Id }, article);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Region region, Guid id, ArticleRequest request)
    {

        await using var db = router.Open(region);
        var article = await db.Articles.FindAsync(id);
        if (article is null)
        {
            logger.LogWarning("Article {ArticleId} not found in {Region}, nothing updated", id, region);
            return NotFound();
        }

        article.Title = request.Title;
        article.Content = request.Content;
        article.Author = request.Author;
        await db.SaveChangesAsync();
        logger.LogInformation("Article {ArticleId} updated in {Region}", id, region);

        if (region == Region.Global)
        {
            await cache.RemoveAsync(id);   // ensuring we dont have outdated cache
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Region region, Guid id)
    {

        await using var db = router.Open(region);
        var deleted = await db.Articles.Where(a => a.Id == id).ExecuteDeleteAsync();
        if (deleted == 0)
        {
            logger.LogWarning("Article {ArticleId} not found in {Region}, nothing deleted", id, region);
            return NotFound();
        }

        logger.LogInformation("Article {ArticleId} deleted from {Region}", id, region);

        if (region == Region.Global)
        {
            await cache.RemoveAsync(id);   // a deleted article must not be in cache
        }

        return NoContent();
    }
}
