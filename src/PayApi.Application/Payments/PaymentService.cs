using Microsoft.EntityFrameworkCore;
using PayApi.Domain.Common;
using PayApi.Domain.Ledger;
using PayApi.Domain.Payments;

namespace PayApi.Application.Payments;

public class PaymentService
{
    private readonly IAppDbContext _db;

    public PaymentService(IAppDbContext db) => _db = db;

    public async Task<PaymentResponse?> GetAsync(Guid merchantId, Guid id, CancellationToken ct)
    {
        var p = await _db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.MerchantId == merchantId, ct);
        return p is null ? null : PaymentResponse.From(p);
    }

    public async Task<IReadOnlyList<LedgerEntryResponse>?> GetLedgerAsync(
        Guid merchantId, Guid id, CancellationToken ct)
    {
        var owns = await _db.Payments.AsNoTracking().AnyAsync(x => x.Id == id && x.MerchantId == merchantId, ct);
        if (!owns) return null;
        var entries = await _db.LedgerEntries.AsNoTracking()
            .Where(e => e.PaymentId == id).OrderBy(e => e.CreatedAt).ToListAsync(ct);
        return entries.Select(LedgerEntryResponse.From).ToList();
    }

    public async Task<PagedResult<PaymentResponse>> ListAsync(
        Guid merchantId, string? status, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Payments.AsNoTracking().Where(p => p.MerchantId == merchantId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<PaymentStatus>(status, true, out var parsed))
                throw new DomainException($"Unknown status '{status}'.");
            query = query.Where(p => p.Status == parsed);
        }
        if (from is not null) { var f = ToUtc(from.Value); query = query.Where(p => p.CreatedAt >= f); }
        if (to is not null) { var t = ToUtc(to.Value); query = query.Where(p => p.CreatedAt <= t); }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<PaymentResponse>(
            items.Select(PaymentResponse.From).ToList(), page, pageSize, total);
    }

    public Task<PaymentResponse?> CaptureAsync(Guid merchantId, Guid id, CancellationToken ct) =>
        MutateAsync(merchantId, id, p => p.Capture(),
            p => LedgerEntry.CreatePair(p.Id, LedgerAccounts.CustomerFunds,
                LedgerAccounts.MerchantBalance, p.AmountCaptured, p.Currency, "capture"), ct);

    public Task<PaymentResponse?> VoidAsync(Guid merchantId, Guid id, CancellationToken ct) =>
        MutateAsync(merchantId, id, p => p.Void(), null, ct);

    public Task<PaymentResponse?> RefundAsync(Guid merchantId, Guid id, long amount, CancellationToken ct) =>
        MutateAsync(merchantId, id, p => p.Refund(amount),
            p => LedgerEntry.CreatePair(p.Id, LedgerAccounts.MerchantBalance,
                LedgerAccounts.CustomerFunds, amount, p.Currency, "refund"), ct);

    private async Task<PaymentResponse?> MutateAsync(Guid merchantId, Guid id, Action<Payment> change,
        Func<Payment, IEnumerable<LedgerEntry>>? ledger, CancellationToken ct)
    {
        var p = await _db.Payments.FirstOrDefaultAsync(x => x.Id == id && x.MerchantId == merchantId, ct);
        if (p is null) return null;

        change(p);
        if (ledger is not null) _db.LedgerEntries.AddRange(ledger(p));

        try
        {
            await _db.SaveChangesAsync(ct); // payment + ledger rows commit in ONE transaction
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("Payment was modified by another request. Please retry.");
        }
        return PaymentResponse.From(p);
    }

    private static DateTime ToUtc(DateTime d) =>
        d.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : d.ToUniversalTime();
}
