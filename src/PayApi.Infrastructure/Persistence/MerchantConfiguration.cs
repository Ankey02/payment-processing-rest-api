using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayApi.Domain.Merchants;

namespace PayApi.Infrastructure.Persistence;

public class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> b)
    {
        b.ToTable("merchants");
        b.HasKey(m => m.Id);
        b.Property(m => m.Name).HasMaxLength(200).IsRequired();
    }
}

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> b)
    {
        b.ToTable("api_keys");
        b.HasKey(k => k.Id);
        b.Property(k => k.KeyHash).HasMaxLength(64).IsRequired();
        b.Property(k => k.Prefix).HasMaxLength(16).IsRequired();
        b.HasIndex(k => k.KeyHash).IsUnique();
    }
}
