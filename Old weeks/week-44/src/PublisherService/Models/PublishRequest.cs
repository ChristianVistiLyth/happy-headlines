using System.ComponentModel.DataAnnotations;

namespace PublisherService.Models;

/// <summary>What the Webapp sends to publish an article. The region decides which database stores it.</summary>
public record PublishRequest(
    [Required, MaxLength(200)] string Title,
    [Required] string Content,
    [Required, MaxLength(100)] string Author,
    [Required] Region? Region);
