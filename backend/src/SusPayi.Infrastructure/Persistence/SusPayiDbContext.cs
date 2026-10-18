using Microsoft.EntityFrameworkCore;
using SusPayi.Domain.Accounts;
using SusPayi.Domain.Actions;
using SusPayi.Domain.Characters;
using SusPayi.Domain.Seasons;

namespace SusPayi.Infrastructure.Persistence;

public sealed class SusPayiDbContext(DbContextOptions<SusPayiDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Season> Seasons => Set<Season>();

    public DbSet<Character> Characters => Set<Character>();

    public DbSet<ActionLogEntry> ActionLog => Set<ActionLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SusPayiDbContext).Assembly);
    }
}
