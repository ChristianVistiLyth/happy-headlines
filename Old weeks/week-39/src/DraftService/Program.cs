using DraftService.Data;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Central logging and tracing (shared Monitoring library)
builder.AddMonitoring();

var connectionString = builder.Configuration.GetConnectionString("DraftDatabase")
    ?? throw new InvalidOperationException("Missing connection string 'DraftDatabase'");

// We don't use Kerberos login; without this the database driver probes for it and logs an error
var connection = new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable };
builder.Services.AddDbContext<DraftDbContext>(options => options.UseNpgsql(connection.ConnectionString));
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Create the Drafts table before taking requests
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DraftDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "DraftService"));   // /swagger - API docs page
}

app.MapControllers();

app.Run();
