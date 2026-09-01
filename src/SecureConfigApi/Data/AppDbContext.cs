using Microsoft.EntityFrameworkCore;
using SecureConfigApi.Models;

namespace SecureConfigApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ConfigEntry> ConfigEntries => Set<ConfigEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfigEntry>()
            .HasIndex(c => new { c.Key, c.Environment })
            .IsUnique();
    }
}
