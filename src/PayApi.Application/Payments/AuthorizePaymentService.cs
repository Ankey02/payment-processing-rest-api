using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PayApi.Domain.Idempotency;
using PayApi.Domain.Payments;

namespace PayApi.Application.Payments;

public class AuthorizePaymentService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IAppDbContext _db;

    public AuthorizePaymentService(IAppDbContext db) => _db = db;

    public async Task<IdempotentResult> ExecuteAsync(
        Guid merchantId, string key, AuthorizePaymentRequest request, CancellationToken ct = default)
    {
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request, Json))));

        var existing = await FindRecordAsync(merchantId, key, ct);
        if (existing is not null) return Replay(existing, hash);

        var payment = Payment.Authorize(merchantId, request.Amount, request.Currency);
        var body = JsonSerializer.Serialize(PaymentResponse.From(payment), Json);

        _db.Payments.Add(payment);
        _db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            MerchantId = merchantId,
            Key = key,
            RequestHash = hash,
            StatusCode = 201,
            ResponseBody = body,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync(ct);
            return new IdempotentResult(201, body, false);
        }
        catch (DbUpdateException)
        {
            // A parallel request with the same key won the race: return its saved response.
            var winner = await FindRecordAsync(merchantId, key, ct);
            if (winner is null) throw;
            return Replay(winner, hash);
        }
    }

    private Task<IdempotencyRecord?> FindRecordAsync(Guid merchantId, string key, CancellationToken ct) =>
        _db.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.MerchantId == merchantId && r.Key == key, ct);

    private static IdempotentResult Replay(IdempotencyRecord r, string hash) =>
        r.RequestHash == hash
            ? new IdempotentResult(r.StatusCode, r.ResponseBody, true)
            : new IdempotentResult(422,
                "{\"error\":\"Idempotency-Key was already used with a different request.\"}", false);
}
