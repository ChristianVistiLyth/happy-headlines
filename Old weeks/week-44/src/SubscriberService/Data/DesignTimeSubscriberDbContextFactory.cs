using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SubscriberService.Data;

/// <summary>Only used by "dotnet ef migrations add".</summary>
public class DesignTimeSubscriberDbContextFactory : IDesignTimeDbContextFactory<SubscriberDbContext>
{
    public SubscriberDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SubscriberDbContext>().UseNpgsql("Host=localhost;Database=subscribers").Options);
}
