namespace NewsletterService.Clients;

/// <summary>Asks ArticleService (through the load balancer) for the articles of one region.</summary>
public class ArticleClient(HttpClient http)
{
    public async Task<List<ArticleSummary>> GetArticlesAsync(string region, CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<List<ArticleSummary>>($"api/regions/{region}/articles", cancellationToken)
        ?? [];
}

// NewsletterService's own copy of the article fields it needs
public record ArticleSummary(Guid Id, DateTime PublishedAt);
