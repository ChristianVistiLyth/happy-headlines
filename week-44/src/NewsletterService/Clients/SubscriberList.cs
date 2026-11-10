using Polly.CircuitBreaker;
using Polly.Timeout;

namespace NewsletterService.Clients;

/// <summary>
/// Gets the subscribers from SubscriberService - which lives in another swimlane.
/// Fault isolation: every call goes through the circuit breaker set up in Program.cs, and when SubscriberService
/// is down (or the circuit is open) we fall back to the last list we received. A failure in the Subscriber
/// swimlane therefore never stops a newsletter.
/// </summary>
public class SubscriberList(IHttpClientFactory httpClients, ILogger<SubscriberList> logger)
{
    public const string HttpClientName = "subscriber-service";

    // The last list SubscriberService gave us. Empty until the first successful call.
    private volatile IReadOnlyList<SubscriberContact> _lastKnown = [];

    public async Task<IReadOnlyList<SubscriberContact>> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var http = httpClients.CreateClient(HttpClientName);
            var subscribers = await http.GetFromJsonAsync<List<SubscriberContact>>("api/subscribers", cancellationToken) ?? [];
            _lastKnown = subscribers;
            return subscribers;
        }
        catch (Exception e) when (e is BrokenCircuitException or TimeoutRejectedException or HttpRequestException)
        {
            // Plan B: use the last known list instead of failing the newsletter
            logger.LogWarning("SubscriberService unavailable ({Reason}), using the last known list of {Count} subscribers",
                e.GetType().Name, _lastKnown.Count);
            return _lastKnown;
        }
    }
}

// NewsletterService's own copy of the subscriber fields it needs
public record SubscriberContact(Guid Id, string Email);
