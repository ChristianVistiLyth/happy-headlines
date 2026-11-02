using System.ComponentModel.DataAnnotations;

namespace CommentService.Models;

public class Comment
{
    public Guid Id { get; set; }

    public Guid ArticleId { get; set; }

    [MaxLength(100)]
    public required string Author { get; set; }

    // Always the filtered text: an unchecked comment is never saved
    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; }
}
