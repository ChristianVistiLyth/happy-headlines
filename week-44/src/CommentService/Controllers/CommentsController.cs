using System.Diagnostics;
using CommentService.Caching;
using CommentService.Clients;
using CommentService.Data;
using CommentService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace CommentService.Controllers;

[ApiController]
[Route("api/articles/{articleId:guid}/comments")]
public class CommentsController(CommentDbContext db, ProfanityClient profanity, CommentCache cache,
    ILogger<CommentsController> logger) : ControllerBase
{
    // Reading comments never calls ProfanityService, so it keeps working when ProfanityService is down
    [HttpGet]
    public async Task<List<Comment>> GetAll(Guid articleId)
    {
        var timer = Stopwatch.StartNew();   // how long the read takes, for the dashboard

        // Cache miss approach: look in the cache first, and only on a miss read the database and fill the cache
        var cached = await cache.TryGetAsync(articleId);
        if (cached is not null)
        {
            CacheMetrics.Record("comment", hit: true, timer.Elapsed);
            return cached;
        }

        var comments = await db.Comments.AsNoTracking()
            .Where(c => c.ArticleId == articleId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        await cache.AddAsync(articleId, comments);
        CacheMetrics.Record("comment", hit: false, timer.Elapsed);   // a miss: the trip to the database is included
        return comments;
    }

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
        logger.LogInformation("Comment {CommentId} saved on article {ArticleId} (profanity masked: {ContainedProfanity})",
            comment.Id, articleId, filtered.ContainedProfanity);

        // A new comment: the cached list is out of date, so the next read loads it again from the database
        await cache.RemoveAsync(articleId);

        return CreatedAtAction(nameof(GetAll), new { articleId }, comment);
    }
}
