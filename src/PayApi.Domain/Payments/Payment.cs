using PayApi.Domain.Common;

namespace PayApi.Domain.Payments;

public class Payment
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }
    public long Amount { get; private set; }
    public long AmountCaptured { get; private set; }
    public long AmountRefunded { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Payment() { }

    public static Payment Authorize(Guid merchantId, long amount, string currency)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter code.");

        var now = DateTime.UtcNow;
        return new Payment
        {
            Id = Guid.NewGuid(),
            MerchantId = merchantId,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            Status = PaymentStatus.Authorized,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Capture()
    {
        if (Status != PaymentStatus.Authorized)
            throw new DomainException($"Cannot capture a payment in status {Status}.");
        AmountCaptured = Amount;
        Status = PaymentStatus.Captured;
        Touch();
    }

    public void Void()
    {
        if (Status != PaymentStatus.Authorized)
            throw new DomainException($"Cannot void a payment in status {Status}.");
        Status = PaymentStatus.Voided;
        Touch();
    }

    public void Refund(long amount)
    {
        if (Status != PaymentStatus.Captured && Status != PaymentStatus.PartiallyRefunded)
            throw new DomainException($"Cannot refund a payment in status {Status}.");
        if (amount <= 0)
            throw new DomainException("Refund amount must be greater than zero.");
        if (amount > AmountCaptured - AmountRefunded)
            throw new DomainException("Refund exceeds the remaining captured amount.");

        AmountRefunded += amount;
        Status = AmountRefunded == AmountCaptured
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
