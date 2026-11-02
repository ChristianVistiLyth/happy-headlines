using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProfanityService.Data;

/// <summary>Only used by "dotnet ef migrations add".</summary>
public class DesignTimeProfanityDbContextFactory : IDesignTimeDbContextFactory<ProfanityDbContext>
{
    public ProfanityDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ProfanityDbContext>().UseNpgsql("Host=localhost;Database=profanity").Options);
}
