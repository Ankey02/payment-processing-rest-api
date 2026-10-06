namespace PayApi.Domain.Payments;

public enum PaymentStatus
{
    Authorized,
    Captured,
    Voided,
    PartiallyRefunded,
    Refunded
}
