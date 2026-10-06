using PayApi.Domain.Payments;

namespace PayApi.Application.Payments;

public record AuthorizePaymentRequest(long Amount, string Currency);
public record RefundRequest(long Amount);

public record PaymentResponse(Guid Id, long Amount, long AmountCaptured, long AmountRefunded,
    string Currency, string Status, DateTime CreatedAt)
{
    public static PaymentResponse From(Payment p) =>
        new(p.Id, p.Amount, p.AmountCaptured, p.AmountRefunded, p.Currency, p.Status.ToString(), p.CreatedAt);
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
public record IdempotentResult(int StatusCode, string Body, bool Replayed);
