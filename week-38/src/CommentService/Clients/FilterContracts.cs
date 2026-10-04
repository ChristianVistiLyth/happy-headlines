namespace CommentService.Clients;

// CommentService's own copy of ProfanityService's request and response

public record FilterRequest(string Text);

public record FilterResponse(string FilteredText, bool ContainedProfanity);
