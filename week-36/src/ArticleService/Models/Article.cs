using System.ComponentModel.DataAnnotations;

namespace ArticleService.Models;

public class Article
{
    // Guid instead of int: ids must stay unique across all eight region databases
    public Guid Id { get; set; }

    [MaxLength(200)]
    public required string Title { get; set; }

    public required string Content { get; set; }

    [MaxLength(100)]
    public required string Author { get; set; }

    public DateTime PublishedAt { get; set; }

    public Region Region { get; set; }
}
