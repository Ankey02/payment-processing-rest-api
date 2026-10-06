namespace PayApi.Domain.Ledger;

public enum EntryDirection { Debit, Credit }

public static class LedgerAccounts
{
    public const string CustomerFunds = "customer_funds";
    public const string MerchantBalance = "merchant_balance";
}

public class LedgerEntry
{
    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public string Account { get; private set; } = "";
    public EntryDirection Direction { get; private set; }
    public long Amount { get; private set; }
    public string Currency { get; private set; } = "";
    public string Reason { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }

    private LedgerEntry() { }

    public static LedgerEntry[] CreatePair(Guid paymentId, string debitAccount, string creditAccount,
        long amount, string currency, string reason)
    {
        var now = DateTime.UtcNow;
        return new[]
        {
            New(paymentId, debitAccount, EntryDirection.Debit, amount, currency, reason, now),
            New(paymentId, creditAccount, EntryDirection.Credit, amount, currency, reason, now)
        };
    }

    private static LedgerEntry New(Guid paymentId, string account, EntryDirection dir,
        long amount, string currency, string reason, DateTime now) => new()
    {
        Id = Guid.NewGuid(), PaymentId = paymentId, Account = account, Direction = dir,
        Amount = amount, Currency = currency, Reason = reason, CreatedAt = now
    };
}
