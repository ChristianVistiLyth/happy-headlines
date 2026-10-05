using Monitoring;
using NewsletterService.Clients;

namespace NewsletterService.Newsletters;

/// <summary>Collects the articles of the last 24 hours from all eight regions and sends them as one newsletter.</summary>
public class DailyNewsletter(ArticleClient articles, NewsletterSender sender, ILogger<DailyNewsletter> logger)
{
    private static readonly string[] Regions =
        ["Africa", "Antarctica", "Asia", "Europe", "NorthAmerica", "Oceania", "SouthAmerica", "Global"];

    public async Task<int> SendAsync(CancellationToken cancellationToken)
    {
        // When the daily timer starts this there is no incoming request, so no trace exists yet.
        // This span becomes the root, and the eight calls to ArticleService hang under it: ONE trace, not eight.
        using var activity = Tracing.Source.StartActivity("daily newsletter");

        var since = DateTime.UtcNow.AddHours(-24);
        var articleCount = 0;
        foreach (var region in Regions)
        {
            try
            {
                var latest = await articles.GetArticlesAsync(region, cancellationToken);
                articleCount += latest.Count(a => a.PublishedAt >= since);
            }
            catch (HttpRequestException e)
            {
                // One region missing shouldn't stop the whole newsletter
                logger.LogWarning("Daily newsletter: {Region} skipped, ArticleService unavailable ({Reason})",
                    region, e.GetType().Name);
            }
        }

        sender.SendDaily(articleCount);
        return articleCount;
    }
}
