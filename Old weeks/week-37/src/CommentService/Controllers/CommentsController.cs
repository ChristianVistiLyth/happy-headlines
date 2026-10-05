using CommentService.Clients;
using CommentService.Data;
using CommentService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace CommentService.Controllers;

[ApiController]
[Route("api/articles/{articleId:guid}/comments")]
public class CommentsController(CommentDbContext db, ProfanityClient profanity, ILogger<CommentsController> logger)
    : ControllerBase
{
    // Reading comments never calls ProfanityService, so it keeps working when ProfanityService is down
    [HttpGet]
    public async Task<List<Comment>> GetAll(Guid articleId) =>
        await db.Comments.AsNoTracking()
            .Where(c => c.ArticleId == articleId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<Comment>> Create(Guid articleId, CommentRequest request,
        CancellationToken cancellationToken)
    {
        FilterResponse filtered;
        try
        {
            filtered = await profanity.FilterAsync(request.Content, cancellationToken);
        }
        catch (Exception e) when (e is BrokenCircuitException or TimeoutRejectedException or HttpRequestException)
        {
            // Plan B: ProfanityService is unavailable (or the circuit breaker is open).
            // An unchecked comment is never saved, so the reader is asked to try again later.
            logger.LogWarning("Comment rejected because ProfanityService is unavailable ({Reason})", e.GetType().Name);
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Comments can't be checked for profanity right now. Please try again later.");
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            ArticleId = articleId,
            Author = request.Author,
            Content = filtered.FilteredText,
            CreatedAt = DateTime.UtcNow
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { articleId }, comment);
    }
}
