using Microsoft.EntityFrameworkCore;
using PayApi.Domain.Idempotency;
using PayApi.Domain.Ledger;
using PayApi.Domain.Payments;

namespace PayApi.Application;

public interface IAppDbContext
{
    DbSet<Payment> Payments { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
