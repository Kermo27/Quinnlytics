using Microsoft.EntityFrameworkCore;
using Quinnlytics.Models;

namespace Quinnlytics.Data;

public class AppDbContext : DbContext
{
    public DbSet<Match> Matches { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<GameVersion> GameVersions { get; set; }

    public AppDbContext()
        : base() { }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=quinnlytics.db");
        }
    }
}
