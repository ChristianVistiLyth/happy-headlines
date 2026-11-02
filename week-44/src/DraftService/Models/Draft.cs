using System.ComponentModel.DataAnnotations;

namespace DraftService.Models;

/// <summary>An article a publisher is still working on.</summary>
public class Draft
{
    public Guid Id { get; set; }

    [MaxLength(200)]
    public required string Title { get; set; }

    public required string Content { get; set; }

    [MaxLength(100)]
    public required string Author { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
