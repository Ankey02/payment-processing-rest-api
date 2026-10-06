using Microsoft.AspNetCore.Mvc;
using PayApi.Application.Payments;
using PayApi.Domain.Common;

namespace PayApi.Api.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private const string MerchantHeader = "X-Merchant-Id";
    private readonly AuthorizePaymentService _authorize;
    private readonly PaymentService _payments;

    public PaymentsController(AuthorizePaymentService authorize, PaymentService payments)
    {
        _authorize = authorize;
        _payments = payments;
    }

    [HttpPost]
    public async Task<IActionResult> Authorize(
        [FromBody] AuthorizePaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = MerchantHeader)] Guid? merchantId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { error = "Idempotency-Key header is required." });
        if (merchantId is null) return MissingMerchant();

        try
        {
            var result = await _authorize.ExecuteAsync(merchantId.Value, idempotencyKey, request, ct);
            Response.Headers["Idempotent-Replayed"] = result.Replayed.ToString().ToLowerInvariant();
            return new ContentResult
            {
                StatusCode = result.StatusCode,
                Content = result.Body,
                ContentType = "application/json"
            };
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(
        Guid id, [FromHeader(Name = MerchantHeader)] Guid? merchantId, CancellationToken ct)
    {
        if (merchantId is null) return MissingMerchant();
        var p = await _payments.GetAsync(merchantId.Value, id, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromHeader(Name = MerchantHeader)] Guid? merchantId,
        [FromQuery] string? status,
        [FromQuery(Name = "from")] DateTime? createdFrom,
        [FromQuery(Name = "to")] DateTime? createdTo,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (merchantId is null) return MissingMerchant();
        try
        {
            return Ok(await _payments.ListAsync(
                merchantId.Value, status, createdFrom, createdTo, page, pageSize, ct));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/capture")]
    public Task<IActionResult> Capture(
        Guid id, [FromHeader(Name = MerchantHeader)] Guid? merchantId, CancellationToken ct) =>
        Transition(merchantId, m => _payments.CaptureAsync(m, id, ct));

    [HttpPost("{id:guid}/void")]
    public Task<IActionResult> Void(
        Guid id, [FromHeader(Name = MerchantHeader)] Guid? merchantId, CancellationToken ct) =>
        Transition(merchantId, m => _payments.VoidAsync(m, id, ct));

    [HttpPost("{id:guid}/refund")]
    public Task<IActionResult> Refund(
        Guid id, [FromBody] RefundRequest request,
        [FromHeader(Name = MerchantHeader)] Guid? merchantId, CancellationToken ct) =>
        Transition(merchantId, m => _payments.RefundAsync(m, id, request.Amount, ct));

    private async Task<IActionResult> Transition(
        Guid? merchantId, Func<Guid, Task<PaymentResponse?>> action)
    {
        if (merchantId is null) return MissingMerchant();
        try
        {
            var result = await action(merchantId.Value);
            return result is null ? NotFound() : Ok(result);
        }
        catch (DomainException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private IActionResult MissingMerchant() =>
        BadRequest(new { error = $"{MerchantHeader} header is required." });
}
