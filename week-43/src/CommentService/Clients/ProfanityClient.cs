namespace CommentService.Clients;

/// <summary>
/// Calls ProfanityService directly. Every call goes through the circuit breaker
/// that is set up for this client in Program.cs.
/// </summary>
public class ProfanityClient(HttpClient http)
{
    public async Task<FilterResponse> FilterAsync(string text, CancellationToken cancellationToken)
    {
        var response = await http.PostAsJsonAsync("api/profanity/filter", new FilterRequest(text), cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FilterResponse>(cancellationToken)
            ?? throw new InvalidOperationException("ProfanityService returned an empty response");
    }
}
