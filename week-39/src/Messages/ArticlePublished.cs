namespace Messages;

/// <summary>
/// Put on the ArticleQueue by PublisherService when an article is published.
/// ArticleService stores it, NewsletterService sends it out.
/// Shared by all three services, because EasyNetQ recognises a message by its .NET type.
/// </summary>
public class ArticlePublished
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public required string Author { get; init; }
    public required string Region { get; init; }
    public DateTime PublishedAt { get; init; }

    /// <summary>
    /// The trace context (traceparent) travels here. A queue message has no HTTP headers,
    /// so we carry it ourselves - otherwise the trace breaks at the queue.
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = new();
}
