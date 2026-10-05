using CommentService.Clients;
using CommentService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Monitoring;
using Npgsql;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Central logging and tracing (shared Monitoring library)
builder.AddMonitoring();

var connectionString = builder.Configuration.GetConnectionString("CommentDatabase")
    ?? throw new InvalidOperationException("Missing connection string 'CommentDatabase'");

// We don't use Kerberos login; without this the database driver probes for it and logs an error
var connection = new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable };
builder.Services.AddDbContext<CommentDbContext>(options => options.UseNpgsql(connection.ConnectionString));

var profanityServiceUrl = builder.Configuration["ProfanityService:BaseUrl"]
    ?? throw new InvalidOperationException("Missing setting 'ProfanityService:BaseUrl'");

builder.Services.AddHttpClient<ProfanityClient>(client => client.BaseAddress = new Uri(profanityServiceUrl))
    .AddResilienceHandler("profanity-circuit-breaker", (pipeline, context) =>
    {
        var logger = context.ServiceProvider.GetRequiredService<ILogger<ProfanityClient>>();

        // Circuit breaker: when ProfanityService keeps failing, stop calling it for a while
        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(10), // look at the calls from the last 10 seconds,
            MinimumThroughput = 3,                        // and when there have been at least 3 of them
            FailureRatio = 0.5,                           // open the circuit if half of them failed.
            BreakDuration = TimeSpan.FromSeconds(15),     // Stay open for 15 seconds, then let one test call through.
            OnOpened = args =>
            {
                logger.LogWarning("Circuit OPEN: ProfanityService is failing, comments are rejected for {Seconds} seconds",
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

        // Each call may take at most 2 seconds; a timeout counts as a failure for the circuit breaker
        pipeline.AddTimeout(TimeSpan.FromSeconds(2));
    });

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Create the Comments table before taking requests
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<CommentDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "CommentService"));   // /swagger - API docs page
}

app.MapControllers();

app.Run();
