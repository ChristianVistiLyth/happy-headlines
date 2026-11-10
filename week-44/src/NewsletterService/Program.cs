using EasyNetQ;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.FeatureManagement;
using Monitoring;
using NewsletterService.Clients;
using NewsletterService.Messaging;
using NewsletterService.Newsletters;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Central logging and tracing (shared Monitoring library)
builder.AddMonitoring();

// Release toggle: read from the mounted toggles.json and re-read while running, so the toggle can be flipped
// without any redeploy. Toggles:File points at the file; when it is missing, the toggle counts as OFF.
builder.Configuration.AddJsonFile(builder.Configuration["Toggles:File"] ?? "toggles.json", optional: true, reloadOnChange: true);
builder.Services.AddFeatureManagement();

// Immediate newsletter: every published article arrives on the ArticleQueue (RabbitMQ)
var rabbitMq = builder.Configuration["RabbitMQ:ConnectionString"]
    ?? throw new InvalidOperationException("Missing setting 'RabbitMQ:ConnectionString'");
builder.Services.AddEasyNetQ(rabbitMq).UseSystemTextJson();
builder.Services.AddHostedService<ArticleQueueSubscriber>();

// Welcome mail: every new subscriber arrives on the SubscriberQueue (same RabbitMQ)
builder.Services.AddHostedService<SubscriberQueueSubscriber>();

// Daily newsletter: asks ArticleService (through the load balancer) for the latest articles
var articleServiceUrl = builder.Configuration["ArticleService:BaseUrl"]
    ?? throw new InvalidOperationException("Missing setting 'ArticleService:BaseUrl'");
builder.Services.AddHttpClient<ArticleClient>(client => client.BaseAddress = new Uri(articleServiceUrl));
builder.Services.AddTransient<DailyNewsletter>();
builder.Services.AddHostedService<DailyNewsletterTimer>();

// Subscribers: asked from SubscriberService (another swimlane) before every newsletter.
// Fault isolation: same circuit breaker as CommentService -> ProfanityService (week 37), and SubscriberList
// falls back to the last list it received, so a dead SubscriberService never stops a newsletter.
var subscriberServiceUrl = builder.Configuration["SubscriberService:BaseUrl"]
    ?? throw new InvalidOperationException("Missing setting 'SubscriberService:BaseUrl'");
builder.Services.AddHttpClient(SubscriberList.HttpClientName, client => client.BaseAddress = new Uri(subscriberServiceUrl))
    .AddResilienceHandler("subscriber-circuit-breaker", (pipeline, context) =>
    {
        var logger = context.ServiceProvider.GetRequiredService<ILogger<SubscriberList>>();

        // Circuit breaker: when SubscriberService keeps failing, stop calling it for a while
        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(10), // look at the calls from the last 10 seconds,
            MinimumThroughput = 3,                        // and when there have been at least 3 of them
            FailureRatio = 0.5,                           // open the circuit if half of them failed.
            BreakDuration = TimeSpan.FromSeconds(15),     // Stay open for 15 seconds, then let one test call through.
            OnOpened = args =>
            {
                logger.LogWarning("Circuit OPEN: SubscriberService is failing, using the last known subscribers for {Seconds} seconds",
                    args.BreakDuration.TotalSeconds);
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = _ =>
            {
                logger.LogInformation("Circuit HALF-OPEN: trying SubscriberService again");
                return ValueTask.CompletedTask;
            },
            OnClosed = _ =>
            {
                logger.LogInformation("Circuit CLOSED: SubscriberService is working again");
                return ValueTask.CompletedTask;
            }
        });

        // Each call may take at most 2 seconds; a timeout counts as a failure for the circuit breaker
        pipeline.AddTimeout(TimeSpan.FromSeconds(2));
    });
builder.Services.AddSingleton<SubscriberList>();

builder.Services.AddSingleton<NewsletterSender>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "NewsletterService"));   // /swagger - API docs page
}

app.MapControllers();

app.Run();
