using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DraftService.Data;

/// <summary>Only used by "dotnet ef migrations add".</summary>
public class DesignTimeDraftDbContextFactory : IDesignTimeDbContextFactory<DraftDbContext>
{
    public DraftDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<DraftDbContext>().UseNpgsql("Host=localhost;Database=drafts").Options);
}
