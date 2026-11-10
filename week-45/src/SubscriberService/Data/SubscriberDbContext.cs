using Microsoft.EntityFrameworkCore;
using SubscriberService.Models;

namespace SubscriberService.Data;

public class SubscriberDbContext(DbContextOptions<SubscriberDbContext> options) : DbContext(options)
{
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The database itself guarantees that an email address is only subscribed once
        modelBuilder.Entity<Subscriber>().HasIndex(s => s.Email).IsUnique();
    }
}
