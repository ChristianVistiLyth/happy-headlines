using ArticleCacheFiller;
using ArticleCacheFiller.Data;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Npgsql;
using OpenTelemetry.Trace;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// Central logging and tracing (shared Monitoring library)
builder.AddMonitoring();

// Reads from the Global article database. ArticleService owns the table; the filler only reads it.
var databaseConnection = builder.Configuration.GetConnectionString("GlobalArticleDatabase")
    ?? throw new InvalidOperationException("Missing connection string 'GlobalArticleDatabase'");
var connection = new NpgsqlConnectionStringBuilder(databaseConnection) { GssEncryptionMode = GssEncryptionMode.Disable };
builder.Services.AddDbContextFactory<ArticleDbContext>(options => options.UseNpgsql(connection.ConnectionString));

// Writes to the ArticleCache (Redis)
var cacheOptions = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("ArticleCache")
    ?? throw new InvalidOperationException("Missing connection string 'ArticleCache'"));
cacheOptions.AbortOnConnectFail = false;           // start even when Redis is down, and reconnect when it is back
cacheOptions.BacklogPolicy = BacklogPolicy.FailFast; // while Redis is down, fail at once instead of waiting
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(cacheOptions));
builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddRedisInstrumentation());   // Redis calls in traces

builder.Services.AddHostedService<FillWorker>();

builder.Build().Run();
