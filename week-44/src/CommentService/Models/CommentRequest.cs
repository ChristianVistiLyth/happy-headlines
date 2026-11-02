using System.ComponentModel.DataAnnotations;

namespace CommentService.Models;

/// <summary>What a reader sends to post a comment.</summary>
public record CommentRequest(
    [Required, MaxLength(100)] string Author,
    [Required, MaxLength(2000)] string Content);
