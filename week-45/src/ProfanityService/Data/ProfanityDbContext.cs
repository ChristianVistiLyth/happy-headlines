using Microsoft.EntityFrameworkCore;
using ProfanityService.Models;

namespace ProfanityService.Data;

public class ProfanityDbContext(DbContextOptions<ProfanityDbContext> options) : DbContext(options)
{
    public DbSet<Word> Words => Set<Word>();
}
