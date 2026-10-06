using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PayApi.Domain.Common;
using PayApi.Domain.Merchants;

namespace PayApi.Application.Merchants;

public record CreateMerchantRequest(string Name);
public record CreatedMerchantResponse(Guid MerchantId, string Name, string ApiKey);

public static class ApiKeyHasher
{
    public static string Hash(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();
}

public class MerchantService
{
    private readonly IAppDbContext _db;

    public MerchantService(IAppDbContext db) => _db = db;

    public async Task<CreatedMerchantResponse> CreateAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");

        var merchant = Merchant.Create(name.Trim());
        var rawKey = "pk_test_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var apiKey = ApiKey.Create(merchant.Id, ApiKeyHasher.Hash(rawKey), rawKey[..12]);

        _db.Merchants.Add(merchant);
        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync(ct);

        // The raw key is returned ONCE here. Only its hash is stored.
        return new CreatedMerchantResponse(merchant.Id, merchant.Name, rawKey);
    }

    public async Task<Guid?> FindMerchantIdAsync(string rawKey, CancellationToken ct)
    {
        var hash = ApiKeyHasher.Hash(rawKey);
        return await _db.ApiKeys.AsNoTracking()
            .Where(k => k.KeyHash == hash && !k.Revoked)
            .Select(k => (Guid?)k.MerchantId)
            .FirstOrDefaultAsync(ct);
    }
}
