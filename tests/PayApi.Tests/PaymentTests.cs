using PayApi.Domain.Common;
using PayApi.Domain.Payments;
using Xunit;

namespace PayApi.Tests;

public class PaymentTests
{
    private static Payment NewPayment(long amount = 1000) =>
        Payment.Authorize(Guid.NewGuid(), amount, "usd");

    [Fact]
    public void Authorize_ValidInput_CreatesAuthorizedPayment()
    {
        var p = NewPayment(2500);
        Assert.Equal(PaymentStatus.Authorized, p.Status);
        Assert.Equal(2500, p.Amount);
        Assert.Equal("USD", p.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Authorize_InvalidAmount_Throws(long amount)
    {
        Assert.Throws<DomainException>(() => NewPayment(amount));
    }

    [Fact]
    public void Authorize_InvalidCurrency_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Payment.Authorize(Guid.NewGuid(), 1000, "US"));
    }

    [Fact]
    public void Capture_Authorized_SetsCaptured()
    {
        var p = NewPayment();
        p.Capture();
        Assert.Equal(PaymentStatus.Captured, p.Status);
        Assert.Equal(1000, p.AmountCaptured);
    }

    [Fact]
    public void Capture_Twice_Throws()
    {
        var p = NewPayment();
        p.Capture();
        Assert.Throws<DomainException>(() => p.Capture());
    }

    [Fact]
    public void Void_Authorized_SetsVoided()
    {
        var p = NewPayment();
        p.Void();
        Assert.Equal(PaymentStatus.Voided, p.Status);
    }

    [Fact]
    public void Void_AfterCapture_Throws()
    {
        var p = NewPayment();
        p.Capture();
        Assert.Throws<DomainException>(() => p.Void());
    }

    [Fact]
    public void Refund_Partial_SetsPartiallyRefunded()
    {
        var p = NewPayment();
        p.Capture();
        p.Refund(400);
        Assert.Equal(PaymentStatus.PartiallyRefunded, p.Status);
        Assert.Equal(400, p.AmountRefunded);
    }

    [Fact]
    public void Refund_Full_SetsRefunded()
    {
        var p = NewPayment();
        p.Capture();
        p.Refund(400);
        p.Refund(600);
        Assert.Equal(PaymentStatus.Refunded, p.Status);
    }

    [Fact]
    public void Refund_ExceedsCaptured_Throws()
    {
        var p = NewPayment();
        p.Capture();
        Assert.Throws<DomainException>(() => p.Refund(1001));
    }

    [Fact]
    public void Refund_BeforeCapture_Throws()
    {
        var p = NewPayment();
        Assert.Throws<DomainException>(() => p.Refund(100));
    }
}
