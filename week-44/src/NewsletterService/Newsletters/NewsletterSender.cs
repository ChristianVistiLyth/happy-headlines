using Monitoring;

namespace NewsletterService.Newsletters;

/// <summary>
/// Pretends to send newsletters: there is no SubscriberService or email system yet,
/// so "sending" is one log line inside a "send newsletter" span.
/// </summary>
public class NewsletterSender(ILogger<NewsletterSender> logger)
{
    public void SendImmediate(Guid articleId, string region)
    {
        using var activity = Tracing.Source.StartActivity("send newsletter");
        activity?.SetTag("newsletter.kind", "immediate");
        activity?.SetTag("article.id", articleId);

        logger.LogInformation("Immediate newsletter for article {ArticleId} ({Region}) sent", articleId, region);
    }

    public void SendDaily(int articleCount)
    {
        using var activity = Tracing.Source.StartActivity("send newsletter");
        activity?.SetTag("newsletter.kind", "daily");
        activity?.SetTag("article.count", articleCount);

        logger.LogInformation("Daily newsletter with {ArticleCount} articles sent", articleCount);
    }
}
