using System.Diagnostics;
using EasyNetQ;
using Messages;
using Microsoft.AspNetCore.Mvc;
using Monitoring;
using Polly.CircuitBreaker;
using Polly.Timeout;
using PublisherService.Clients;
using PublisherService.Models;

namespace PublisherService.Controllers;

/// <summary>
/// Publishing: filter profanity, then put the article on the ArticleQueue.
/// ArticleService and NewsletterService pick it up from there - PublisherService doesn't wait for them.
/// </summary>
[ApiController]
[Route("api/articles")]
public class ArticlesController(ProfanityClient profanity, IBus bus, ILogger<ArticlesController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PublishResponse>> Publish(PublishRequest request, CancellationToken cancellationToken)
    {
        FilterResponse title, content;
        try
        {
            title = await profanity.FilterAsync(request.Title, cancellationToken);
            content = await profanity.FilterAsync(request.Content, cancellationToken);
        }
        catch (Exception e) when (e is BrokenCircuitException or TimeoutRejectedException or HttpRequestException)
        {
            // An unchecked article is never published, so the publisher is asked to try again later
            logger.LogWarning("Article rejected because ProfanityService is unavailable ({Reason})", e.GetType().Name);
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "The article can't be checked for profanity right now. Please try again later.");
        }

        // PublisherService creates the id, so the same id shows up in the logs of every service
        var article = new ArticlePublished
        {
            Id = Guid.NewGuid(),
            Title = title.FilteredText,
            Content = content.FilteredText,
            Author = request.Author,
            Region = request.Region!.Value.ToString(),
            PublishedAt = DateTime.UtcNow
        };

        try
        {
            using var activity = Tracing.Source.StartActivity("publish article", ActivityKind.Producer);
            activity?.SetTag("article.id", article.Id);

            // The trace context goes INTO the message, so the receivers can continue this trace
            Tracing.Inject(activity, article.Headers);
            await bus.PubSub.PublishAsync(article, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Article {ArticleId} not published because the ArticleQueue is unavailable ({Reason})",
                article.Id, e.GetType().Name);
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "The article can't be published right now. Please try again later.");
        }

        logger.LogInformation("Article {ArticleId} published to the ArticleQueue for {Region} by {Author}",
            article.Id, article.Region, article.Author);

        // 202 Accepted: the article is on its way, ArticleService stores it a moment later
        return Accepted(new PublishResponse(article.Id, article.Region, article.Title, article.PublishedAt));
    }
}
