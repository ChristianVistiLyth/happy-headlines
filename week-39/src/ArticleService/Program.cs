using ArticleService.Controllers;
using ArticleService.Data;
using ArticleService.Messaging;
using EasyNetQ;
using Monitoring;

var builder = WebApplication.CreateBuilder(args);

// Central logging and tracing (shared Monitoring library)
builder.AddMonitoring();

// New articles arrive on the ArticleQueue (RabbitMQ) from PublisherService
var rabbitMq = builder.Configuration["RabbitMQ:ConnectionString"]
    ?? throw new InvalidOperationException("Missing setting 'RabbitMQ:ConnectionString'");
builder.Services.AddEasyNetQ(rabbitMq).UseSystemTextJson();
builder.Services.AddHostedService<ArticleQueueSubscriber>();

builder.Services.AddSingleton<ArticleDatabaseRouter>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<RouteOptions>(options => options.ConstraintMap["region"] = typeof(RegionRouteConstraint));

var app = builder.Build();

// Create the tables in all eight region databases (and add sample articles) before taking requests
await app.Services.GetRequiredService<ArticleDatabaseRouter>().MigrateAllAsync();

// Tell the caller which of the three instances answered, so the load balancer can be seen at work
var instanceName = app.Configuration["InstanceName"] ?? Environment.MachineName;
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Instance"] = instanceName;
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "ArticleService"));   // /swagger - API docs page
}

app.MapControllers();

app.Run();
