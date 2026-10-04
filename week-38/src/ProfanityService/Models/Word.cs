using System.ComponentModel.DataAnnotations;

namespace ProfanityService.Models;

/// <summary>A prohibited word, stored in lower case.</summary>
public class Word
{
    [Key, MaxLength(50)]
    public required string Text { get; set; }
}
