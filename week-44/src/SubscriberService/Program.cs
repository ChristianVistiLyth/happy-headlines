using EasyNetQ;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Monitoring;
using Npgsql;
using SubscriberService.Data;

var builder = WebApplication.CreateBuilder(args);

// Central logging and tracing (shared Monitoring library)
builder.AddMonitoring();

// Release toggle: read from the mounted toggles.json and re-read while running, so the toggle can be flipped
// without any redeploy. Toggles:File points at the file; when it is missing, the toggle counts as OFF.
builder.Configuration.AddJsonFile(builder.Configuration["Toggles:File"] ?? "toggles.json", optional: true, reloadOnChange: true);
builder.Services.AddFeatureManagement();

var connectionString = builder.Configuration.GetConnectionString("SubscriberDatabase")
    ?? throw new InvalidOperationException("Missing connection string 'SubscriberDatabase'");

// We don't use Kerberos login; without this the database driver probes for it and logs an error
var connection = new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable };
builder.Services.AddDbContext<SubscriberDbContext>(options => options.UseNpgsql(connection.ConnectionString));

// The SubscriberQueue lives in RabbitMQ (next to the ArticleQueue); EasyNetQ creates the exchange and queues for us
var rabbitMq = builder.Configuration["RabbitMQ:ConnectionString"]
    ?? throw new InvalidOperationException("Missing setting 'RabbitMQ:ConnectionString'");
builder.Services.AddEasyNetQ(rabbitMq).UseSystemTextJson();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Create the Subscribers table before taking requests
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<SubscriberDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "SubscriberService"));   // /swagger - API docs page
}

app.MapControllers();

app.Run();
