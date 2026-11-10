using EasyNetQ;
using Messages;
using Monitoring;
using NewsletterService.Newsletters;

namespace NewsletterService.Messaging;

/// <summary>
/// Subscribes to the SubscriberQueue and sends a welcome mail to every new subscriber.
/// SubscriberService never waits for this: the queue decouples the two swimlanes.
/// </summary>
public class SubscriberQueueSubscriber(IBus bus, NewsletterSender sender, ILogger<SubscriberQueueSubscriber> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // RabbitMQ may start after us, or be down: keep trying
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await bus.PubSub.SubscribeAsync<SubscriberSubscribed>("NewsletterService", SendWelcomeAsync,
                    subscription => subscription.WithQueueName("SubscriberQueue.NewsletterService"), stoppingToken);
                logger.LogInformation("Subscribed to the SubscriberQueue");
                return;
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("SubscriberQueue not reachable, retrying in 5 seconds ({Reason})", e.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private Task SendWelcomeAsync(SubscriberSubscribed message, CancellationToken cancellationToken)
    {
        // Continue SubscriberService's trace: its trace context travelled inside the message
        using var activity = Tracing.StartReceive("receive subscriber", message.Headers);
        activity?.SetTag("subscriber.id", message.SubscriberId);

        sender.SendWelcome(message.SubscriberId, message.Email);
        return Task.CompletedTask;
    }
}
