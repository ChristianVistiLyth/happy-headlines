using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CommentService.Data;

/// <summary>Only used by "dotnet ef migrations add".</summary>
public class DesignTimeCommentDbContextFactory : IDesignTimeDbContextFactory<CommentDbContext>
{
    public CommentDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CommentDbContext>().UseNpgsql("Host=localhost;Database=comments").Options);
}
