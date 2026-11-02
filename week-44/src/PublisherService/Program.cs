using EasyNetQ;
using Microsoft.Extensions.Http.Resilience;
using Monitoring;
using Polly;
using PublisherService.Clients;

var builder = WebApplication.CreateBuilder(args);

// Central logging and tracing (shared Monitoring library)
builder.AddMonitoring();

// The ArticleQueue lives in RabbitMQ; EasyNetQ creates the exchange and queues for us
var rabbitMq = builder.Configuration["RabbitMQ:ConnectionString"]
    ?? throw new InvalidOperationException("Missing setting 'RabbitMQ:ConnectionString'");
builder.Services.AddEasyNetQ(rabbitMq).UseSystemTextJson();

var profanityServiceUrl = builder.Configuration["ProfanityService:BaseUrl"]
    ?? throw new InvalidOperationException("Missing setting 'ProfanityService:BaseUrl'");

// Same circuit breaker as in CommentService (week 37)
builder.Services.AddHttpClient<ProfanityClient>(client => client.BaseAddress = new Uri(profanityServiceUrl))
    .AddResilienceHandler("profanity-circuit-breaker", (pipeline, context) =>
    {
        var logger = context.ServiceProvider.GetRequiredService<ILogger<ProfanityClient>>();

        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(10),
            MinimumThroughput = 3,
            FailureRatio = 0.5,
            BreakDuration = TimeSpan.FromSeconds(15),
            OnOpened = args =>
            {
                logger.LogWarning("Circuit OPEN: ProfanityService is failing, articles are rejected for {Seconds} seconds",
                    args.BreakDuration.TotalSeconds);
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = _ =>
            {
                logger.LogInformation("Circuit HALF-OPEN: trying ProfanityService again");
                return ValueTask.CompletedTask;
            },
            OnClosed = _ =>
            {
                logger.LogInformation("Circuit CLOSED: ProfanityService is working again");
                return ValueTask.CompletedTask;
            }
        });

        pipeline.AddTimeout(TimeSpan.FromSeconds(2));
    });

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "PublisherService"));   // /swagger - API docs page
}

app.MapControllers();

app.Run();
