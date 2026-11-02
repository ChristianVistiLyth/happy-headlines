namespace NewsletterService.Newsletters;

/// <summary>Sends the daily newsletter once a day at the configured time (UTC), e.g. Newsletter:DailySendTimeUtc = 06:00.</summary>
public class DailyNewsletterTimer(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<DailyNewsletterTimer> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sendTime = TimeOnly.Parse(configuration["Newsletter:DailySendTimeUtc"] ?? "06:00");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var next = now.Date + sendTime.ToTimeSpan();
            if (next <= now) next = next.AddDays(1);
            await Task.Delay(next - now, stoppingToken);

            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<DailyNewsletter>().SendAsync(stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError("Daily newsletter failed ({Reason})", e.GetType().Name);
            }
        }
    }
}
