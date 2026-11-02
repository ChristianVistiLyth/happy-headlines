namespace ProfanityService.Models;

/// <summary>The text with every prohibited word replaced by asterisks.</summary>
public record FilterResponse(string FilteredText, bool ContainedProfanity);
