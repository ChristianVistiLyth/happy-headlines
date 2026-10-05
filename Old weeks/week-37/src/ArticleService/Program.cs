using ArticleService.Controllers;
using ArticleService.Data;

var builder = WebApplication.CreateBuilder(args);

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
