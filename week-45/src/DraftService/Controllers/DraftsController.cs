using DraftService.Data;
using DraftService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DraftService.Controllers;

/// <summary>
/// CRUD for drafts. Logging policy: Information when something changes, Warning when a draft is not found,
/// and only ids and author names are logged - never the draft text.
/// </summary>
[ApiController]
[Route("api/drafts")]
public class DraftsController(DraftDbContext db, ILogger<DraftsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<List<Draft>> GetAll(string? author)
    {
        var drafts = db.Drafts.AsNoTracking();
        if (author is not null)
        {
            drafts = drafts.Where(d => d.Author == author);
        }

        return await drafts.OrderByDescending(d => d.UpdatedAt).ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Draft>> Get(Guid id)
    {
        var draft = await db.Drafts.FindAsync(id);
        if (draft is null)
        {
            logger.LogWarning("Draft {DraftId} not found", id);
            return NotFound();
        }

        return draft;
    }

    [HttpPost]
    public async Task<ActionResult<Draft>> Create(DraftRequest request)
    {
        var now = DateTime.UtcNow;
        var draft = new Draft
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            Author = request.Author,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Drafts.Add(draft);
        await db.SaveChangesAsync();
        logger.LogInformation("Draft {DraftId} created by {Author}", draft.Id, draft.Author);

        return CreatedAtAction(nameof(Get), new { id = draft.Id }, draft);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, DraftRequest request)
    {
        var draft = await db.Drafts.FindAsync(id);
        if (draft is null)
        {
            logger.LogWarning("Draft {DraftId} not found, nothing updated", id);
            return NotFound();
        }

        draft.Title = request.Title;
        draft.Content = request.Content;
        draft.Author = request.Author;
        draft.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        logger.LogInformation("Draft {DraftId} updated by {Author}", draft.Id, draft.Author);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await db.Drafts.Where(d => d.Id == id).ExecuteDeleteAsync();
        if (deleted == 0)
        {
            logger.LogWarning("Draft {DraftId} not found, nothing deleted", id);
            return NotFound();
        }

        logger.LogInformation("Draft {DraftId} deleted", id);
        return NoContent();
    }
}
