using System.ComponentModel.DataAnnotations;

namespace ArticleService.Models;

/// <summary>
/// What a client sends to create or update an article.
/// Id, PublishedAt and Region are set by the service, not by the client.
/// </summary>
public record ArticleRequest(
    [Required, MaxLength(200)] string Title,
    [Required] string Content,
    [Required, MaxLength(100)] string Author);
