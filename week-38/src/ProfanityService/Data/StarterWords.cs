using Microsoft.EntityFrameworkCore;
using ProfanityService.Models;

namespace ProfanityService.Data;

/// <summary>Adds a few mild words to an empty word list, so there is something to filter.</summary>
public static class StarterWords
{
    private static readonly string[] Words = ["darn", "heck", "crap", "damn", "stupid", "idiot"];

    public static async Task SeedAsync(DbContext db, CancellationToken cancellationToken)
    {
        var words = db.Set<Word>();
        if (await words.AnyAsync(cancellationToken))
        {
            return;
        }

        words.AddRange(Words.Select(text => new Word { Text = text }));
        await db.SaveChangesAsync(cancellationToken);
    }
}
