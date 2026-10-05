using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProfanityService.Data;
using ProfanityService.Models;

namespace ProfanityService.Controllers;

/// <summary>Manages the list of prohibited words.</summary>
[ApiController]
[Route("api/words")]
public class WordsController(ProfanityDbContext db) : ControllerBase
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
            return Conflict($"'{text}' is already on the list.");
        }

        db.Words.Add(new Word { Text = text });
        await db.SaveChangesAsync();
        return Created($"/api/words/{text}", text);
    }

    [HttpDelete("{text}")]
    public async Task<IActionResult> Delete(string text)
    {
        var deleted = await db.Words.Where(w => w.Text == text.ToLowerInvariant()).ExecuteDeleteAsync();
        return deleted == 0 ? NotFound() : NoContent();
    }
}
