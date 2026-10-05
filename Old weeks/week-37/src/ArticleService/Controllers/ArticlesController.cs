using ArticleService.Data;
using ArticleService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArticleService.Controllers;

/// <summary>
/// CRUD for articles. The region in the URL decides which of the eight databases is used.
/// </summary>
[ApiController]
[Route("api/regions/{region:region}/articles")]
public class ArticlesController(ArticleDatabaseRouter router) : ControllerBase
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
        await using var db = router.Open(region);
        var article = await db.Articles.FindAsync(id);
        return article is null ? NotFound() : article;
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

        return CreatedAtAction(nameof(Get), new { region, id = article.Id }, article);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Region region, Guid id, ArticleRequest request)
    {
        await using var db = router.Open(region);
        var article = await db.Articles.FindAsync(id);
        if (article is null)
        {
            return NotFound();
        }

        article.Title = request.Title;
        article.Content = request.Content;
        article.Author = request.Author;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Region region, Guid id)
    {
        await using var db = router.Open(region);
        var deleted = await db.Articles.Where(a => a.Id == id).ExecuteDeleteAsync();
        return deleted == 0 ? NotFound() : NoContent();
    }
}
