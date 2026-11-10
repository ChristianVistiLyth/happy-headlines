namespace NewsletterService.Clients;

/// <summary>Asks ArticleService (through the load balancer) for the articles of one region.</summary>
public class ArticleClient(HttpClient http)
{
    // Only the articles published after "since" travel over the network (GAF "Reduce Network Package Size")
    public async Task<List<ArticleSummary>> GetArticlesSinceAsync(string region, DateTime since,
        CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<List<ArticleSummary>>(
            $"api/regions/{region}/articles?since={Uri.EscapeDataString(since.ToString("O"))}", cancellationToken)
        ?? [];
}

// NewsletterService's own copy of the article fields it needs
public record ArticleSummary(Guid Id, DateTime PublishedAt);
