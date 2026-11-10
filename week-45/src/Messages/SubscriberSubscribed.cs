namespace Messages;

/// <summary>
/// Put on the SubscriberQueue by SubscriberService when a reader subscribes.
/// NewsletterService picks it up and sends a welcome mail.
/// Shared by both services, because EasyNetQ recognises a message by its .NET type.
/// </summary>
public class SubscriberSubscribed
{
    public Guid SubscriberId { get; init; }
    public required string Email { get; init; }

    /// <summary>
    /// The trace context (traceparent) travels here, exactly like in <see cref="ArticlePublished"/>,
    /// so subscribing and the welcome mail end up in ONE trace.
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = new();
}
