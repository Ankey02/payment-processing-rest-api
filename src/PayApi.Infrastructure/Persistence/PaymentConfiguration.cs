using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayApi.Domain.Payments;

namespace PayApi.Infrastructure.Persistence;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");
        b.HasKey(p => p.Id);
        b.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.HasIndex(p => new { p.MerchantId, p.CreatedAt });
        b.HasIndex(p => p.Status);

        // Optimistic concurrency: Postgres bumps xmin on every row update.
        b.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
