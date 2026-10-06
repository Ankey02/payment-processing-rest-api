using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayApi.Domain.Ledger;

namespace PayApi.Infrastructure.Persistence;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> b)
    {
        b.ToTable("ledger_entries");
        b.HasKey(e => e.Id);
        b.Property(e => e.Account).HasMaxLength(50).IsRequired();
        b.Property(e => e.Direction).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        b.Property(e => e.Reason).HasMaxLength(30).IsRequired();
        b.HasIndex(e => e.PaymentId);
    }
}
