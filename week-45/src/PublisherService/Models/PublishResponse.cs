namespace PublisherService.Models;

/// <summary>The answer to a publish request: the article is accepted and on its way to ArticleService.</summary>
public record PublishResponse(Guid Id, string Region, string Title, DateTime PublishedAt);
