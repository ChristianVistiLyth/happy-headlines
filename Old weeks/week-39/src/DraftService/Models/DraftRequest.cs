using System.ComponentModel.DataAnnotations;

namespace DraftService.Models;

/// <summary>What a publisher sends to save a draft. Id and timestamps are set by the service.</summary>
public record DraftRequest(
    [Required, MaxLength(200)] string Title,
    [Required] string Content,
    [Required, MaxLength(100)] string Author);
