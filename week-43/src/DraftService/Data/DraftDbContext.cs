using DraftService.Models;
using Microsoft.EntityFrameworkCore;

namespace DraftService.Data;

public class DraftDbContext(DbContextOptions<DraftDbContext> options) : DbContext(options)
{
    public DbSet<Draft> Drafts => Set<Draft>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Publishers look up their own drafts
        modelBuilder.Entity<Draft>().HasIndex(d => d.Author);
    }
}
