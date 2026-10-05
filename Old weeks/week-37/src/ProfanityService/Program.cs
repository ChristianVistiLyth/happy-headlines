using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProfanityService.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ProfanityDatabase")
    ?? throw new InvalidOperationException("Missing connection string 'ProfanityDatabase'");

// We don't use Kerberos login; without this the database driver probes for it and logs an error
var connection = new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable };

builder.Services.AddDbContext<ProfanityDbContext>(options => options
    .UseNpgsql(connection.ConnectionString)
    .UseAsyncSeeding((db, _, cancellationToken) => StarterWords.SeedAsync(db, cancellationToken)));
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Create the Words table (and add the starter words) before taking requests
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ProfanityDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "ProfanityService"));   // /swagger - API docs page
}

app.MapControllers();

app.Run();
