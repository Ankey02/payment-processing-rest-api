using PayApi.Domain.Ledger;

namespace PayApi.Application.Payments;

public record LedgerEntryResponse(Guid Id, string Account, string Direction, long Amount,
    string Currency, string Reason, DateTime CreatedAt)
{
    public static LedgerEntryResponse From(LedgerEntry e) =>
        new(e.Id, e.Account, e.Direction.ToString(), e.Amount, e.Currency, e.Reason, e.CreatedAt);
}
