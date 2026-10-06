namespace PayApi.Domain.Merchants;

public class ApiKey
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }
    public string KeyHash { get; private set; } = "";
    public string Prefix { get; private set; } = "";
    public bool Revoked { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ApiKey() { }

    public static ApiKey Create(Guid merchantId, string keyHash, string prefix) => new()
    {
        Id = Guid.NewGuid(),
        MerchantId = merchantId,
        KeyHash = keyHash,
        Prefix = prefix,
        CreatedAt = DateTime.UtcNow
    };

    public void Revoke() => Revoked = true;
}
