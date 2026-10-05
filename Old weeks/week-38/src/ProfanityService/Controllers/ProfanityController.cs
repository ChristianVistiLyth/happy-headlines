using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProfanityService.Data;
using ProfanityService.Models;

namespace ProfanityService.Controllers;

[ApiController]
[Route("api/profanity")]
public class ProfanityController(ProfanityDbContext db) : ControllerBase
{
    /// <summary>Replaces every prohibited word in the text with asterisks.</summary>
    [HttpPost("filter")]
    public async Task<FilterResponse> Filter(FilterRequest request, CancellationToken cancellationToken)
    {
        var words = await db.Words.Select(w => w.Text).ToListAsync(cancellationToken);
        if (words.Count == 0)
        {
            return new FilterResponse(request.Text, false);
        }

        // Whole words only, in any letter case: "Darn" is caught, "darning" is not
        var pattern = @"\b(" + string.Join("|", words.Select(Regex.Escape)) + @")\b";
        var containedProfanity = false;
        var filteredText = Regex.Replace(request.Text, pattern, match =>
        {
            containedProfanity = true;
            return new string('*', match.Length);
        }, RegexOptions.IgnoreCase);

        return new FilterResponse(filteredText, containedProfanity);
    }
}
