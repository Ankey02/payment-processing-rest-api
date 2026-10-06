using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayApi.Domain.Idempotency;

namespace PayApi.Infrastructure.Persistence;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> b)
    {
        b.ToTable("idempotency_keys");
        b.HasKey(r => new { r.MerchantId, r.Key });
        b.Property(r => r.Key).HasMaxLength(200);
        b.Property(r => r.RequestHash).HasMaxLength(64).IsRequired();
        b.Property(r => r.ResponseBody).IsRequired();
    }
}
