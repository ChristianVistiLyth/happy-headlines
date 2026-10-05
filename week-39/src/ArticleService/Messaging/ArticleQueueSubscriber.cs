using ArticleService.Data;
using ArticleService.Models;
using EasyNetQ;
using Messages;
using Microsoft.EntityFrameworkCore;
using Monitoring;

namespace ArticleService.Messaging;

/// <summary>
/// Subscribes to the ArticleQueue and stores every published article in the database of its region.
/// All three instances use the same subscription id, so they share ONE queue:
/// each article is delivered to exactly one of them and stored once.
/// </summary>
public class ArticleQueueSubscriber(IBus bus, ArticleDatabaseRouter router, ILogger<ArticleQueueSubscriber> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // RabbitMQ may start after us, or be down: keep trying. ArticleService serves HTTP requests meanwhile.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await bus.PubSub.SubscribeAsync<ArticlePublished>("ArticleService", StoreAsync,
                    subscription => subscription.WithQueueName("ArticleQueue.ArticleService"), stoppingToken);
                logger.LogInformation("Subscribed to the ArticleQueue");
                return;
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("ArticleQueue not reachable, retrying in 5 seconds ({Reason})", e.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task StoreAsync(ArticlePublished message, CancellationToken cancellationToken)
    {
        // Continue the publisher's trace: its trace context travelled inside the message
        using var activity = Tracing.StartReceive("receive article", message.Headers);
        activity?.SetTag("article.id", message.Id);

        var region = Enum.Parse<Region>(message.Region, ignoreCase: true);
        try
        {
            await using var db = router.Open(region);

            // The same message can arrive twice (e.g. redelivered after a crash): store it only once
            if (await db.Articles.AnyAsync(a => a.Id == message.Id, cancellationToken))
            {
                logger.LogInformation("Article {ArticleId} is already stored in {Region}, skipped", message.Id, region);
                return;
            }

            db.Articles.Add(new Article
            {
                Id = message.Id,
                Title = message.Title,
                Content = message.Content,
                Author = message.Author,
                PublishedAt = message.PublishedAt,
                Region = region
            });
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Article {ArticleId} from the ArticleQueue stored in {Region}", message.Id, region);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Rethrow: EasyNetQ then moves the message to its error queue, so the article isn't lost
            logger.LogError("Article {ArticleId} could not be stored in {Region}, moved to the error queue ({Reason})",
                message.Id, region, e.GetType().Name);
            throw;
        }
    }
}
