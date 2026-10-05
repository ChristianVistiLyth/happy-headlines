using ArticleService.Data;
using ArticleService.Models;
using ArticleService.Caching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArticleService.Controllers;

/// <summary>
/// CRUD for articles. The region in the URL decides which of the eight databases is used.
/// Logging policy: Information when something changes, Warning when an article is not found, ids only.
/// </summary>
[ApiController]
[Route("api/regions/{region:region}/articles")]
public class ArticlesController(ArticleDatabaseRouter router, ArticleCache cache, ILogger<ArticlesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Article>>> GetAll(Region region)
    {
        await using var db = router.Open(region);
        return await db.Articles.AsNoTracking().OrderByDescending(a => a.PublishedAt).ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Article>> Get(Region region, Guid id)
    {

        if(region == Region.Global)
        {
            var cached = await cache.TryGetAsync(id);

            if(cached!=null)
            {
                return cached;
            }

        }

        await using var db = router.Open(region);
        var article = await db.Articles.FindAsync(id);
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
