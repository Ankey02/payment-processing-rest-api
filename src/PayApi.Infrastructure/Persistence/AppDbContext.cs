using Microsoft.EntityFrameworkCore;
using PayApi.Application;
using PayApi.Domain.Idempotency;
using PayApi.Domain.Ledger;
using PayApi.Domain.Payments;

namespace PayApi.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
