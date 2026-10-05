using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProfanityService.Data;
using ProfanityService.Models;

namespace ProfanityService.Controllers;

/// <summary>
/// Manages the list of prohibited words.
/// Logging policy: Information when the list changes, Warning when a change is refused.
/// </summary>
[ApiController]
[Route("api/words")]
public class WordsController(ProfanityDbContext db, ILogger<WordsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<List<string>> GetAll() =>
        await db.Words.OrderBy(w => w.Text).Select(w => w.Text).ToListAsync();

    [HttpPost]
    public async Task<IActionResult> Add(WordRequest request)
    {
        var text = request.Text.Trim().ToLowerInvariant();
        if (await db.Words.AnyAsync(w => w.Text == text))
        {
            logger.LogWarning("Word {Word} is already on the profanity list", text);
            return Conflict($"'{text}' is already on the list.");
        }

        db.Words.Add(new Word { Text = text });
        await db.SaveChangesAsync();
        logger.LogInformation("Word {Word} added to the profanity list", text);
        return Created($"/api/words/{text}", text);
    }

    [HttpDelete("{text}")]
    public async Task<IActionResult> Delete(string text)
    {
        var word = text.ToLowerInvariant();
        var deleted = await db.Words.Where(w => w.Text == word).ExecuteDeleteAsync();
        if (deleted == 0)
        {
            logger.LogWarning("Word {Word} is not on the profanity list, nothing removed", word);
            return NotFound();
        }

        logger.LogInformation("Word {Word} removed from the profanity list", word);
        return NoContent();
    }
}
