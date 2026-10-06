namespace PayApi.Domain.Idempotency;

public class IdempotencyRecord
{
    public Guid MerchantId { get; set; }
    public string Key { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
