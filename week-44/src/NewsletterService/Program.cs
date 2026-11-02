using EasyNetQ;
using Monitoring;
using NewsletterService.Clients;
using NewsletterService.Messaging;
using NewsletterService.Newsletters;

var builder = WebApplication.CreateBuilder(args);

// Central logging and tracing (shared Monitoring library)
builder.AddMonitoring();

// Immediate newsletter: every published article arrives on the ArticleQueue (RabbitMQ)
var rabbitMq = builder.Configuration["RabbitMQ:ConnectionString"]
    ?? throw new InvalidOperationException("Missing setting 'RabbitMQ:ConnectionString'");
builder.Services.AddEasyNetQ(rabbitMq).UseSystemTextJson();
builder.Services.AddHostedService<ArticleQueueSubscriber>();

// Daily newsletter: asks ArticleService (through the load balancer) for the latest articles
var articleServiceUrl = builder.Configuration["ArticleService:BaseUrl"]
    ?? throw new InvalidOperationException("Missing setting 'ArticleService:BaseUrl'");
builder.Services.AddHttpClient<ArticleClient>(client => client.BaseAddress = new Uri(articleServiceUrl));
builder.Services.AddTransient<DailyNewsletter>();
builder.Services.AddHostedService<DailyNewsletterTimer>();

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
