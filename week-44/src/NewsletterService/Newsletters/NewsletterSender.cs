using Monitoring;
using NewsletterService.Clients;

namespace NewsletterService.Newsletters;

/// <summary>
/// Pretends to send newsletters and welcome mails: there is no email system yet,
/// so "sending" is one log line inside a span.
/// </summary>
public class NewsletterSender(ILogger<NewsletterSender> logger)
{
    public void SendImmediate(Guid articleId, string region, IReadOnlyCollection<SubscriberContact> recipients)
    {
        using var activity = Tracing.Source.StartActivity("send newsletter");
        activity?.SetTag("newsletter.kind", "immediate");
        activity?.SetTag("article.id", articleId);
        activity?.SetTag("newsletter.recipients", recipients.Count);

        logger.LogInformation("Immediate newsletter for article {ArticleId} ({Region}) sent to {RecipientCount} subscribers",
            articleId, region, recipients.Count);
    }

    public void SendDaily(int articleCount, IReadOnlyCollection<SubscriberContact> recipients)
    {
        using var activity = Tracing.Source.StartActivity("send newsletter");
        activity?.SetTag("newsletter.kind", "daily");
        activity?.SetTag("article.count", articleCount);
        activity?.SetTag("newsletter.recipients", recipients.Count);

        logger.LogInformation("Daily newsletter with {ArticleCount} articles sent to {RecipientCount} subscribers",
            articleCount, recipients.Count);
    }

    // The address would be the mail's recipient; it is personal data, so only the subscriber id is logged
    public void SendWelcome(Guid subscriberId, string email)
    {
        using var activity = Tracing.Source.StartActivity("send welcome mail");
        activity?.SetTag("subscriber.id", subscriberId);

        logger.LogInformation("Welcome mail sent to subscriber {SubscriberId}", subscriberId);
    }
}
