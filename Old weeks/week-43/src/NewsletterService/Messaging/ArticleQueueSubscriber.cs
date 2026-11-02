using EasyNetQ;
using Messages;
using Monitoring;
using NewsletterService.Newsletters;

namespace NewsletterService.Messaging;

/// <summary>
/// Subscribes to the ArticleQueue and sends an immediate newsletter for every published article.
/// Its own subscription id gives NewsletterService its own queue, so it gets a copy of EVERY article,
/// next to the copy the ArticleService instances share.
/// </summary>
public class ArticleQueueSubscriber(IBus bus, NewsletterSender sender, ILogger<ArticleQueueSubscriber> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // RabbitMQ may start after us, or be down: keep trying
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await bus.PubSub.SubscribeAsync<ArticlePublished>("NewsletterService", SendImmediateAsync,
                    subscription => subscription.WithQueueName("ArticleQueue.NewsletterService"), stoppingToken);
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

    private Task SendImmediateAsync(ArticlePublished message, CancellationToken cancellationToken)
    {
        // Continue the publisher's trace: its trace context travelled inside the message
        using var activity = Tracing.StartReceive("receive article", message.Headers);
        activity?.SetTag("article.id", message.Id);

        sender.SendImmediate(message.Id, message.Region);
        return Task.CompletedTask;
    }
}
