using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayApi.Api.Auth;
using PayApi.Application.Payments;
using PayApi.Domain.Common;

namespace PayApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly AuthorizePaymentService _authorizeService;
    private readonly PaymentService _payments;

    public PaymentsController(AuthorizePaymentService authorizeService, PaymentService payments)
    {
        _authorizeService = authorizeService;
        _payments = payments;
    }

    [HttpPost]
    public async Task<IActionResult> AuthorizePayment(
        [FromBody] AuthorizePaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { error = "Idempotency-Key header is required." });

        try
        {
            var result = await _authorizeService.ExecuteAsync(User.MerchantId(), idempotencyKey, request, ct);
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
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var p = await _payments.GetAsync(User.MerchantId(), id, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery(Name = "from")] DateTime? createdFrom,
        [FromQuery(Name = "to")] DateTime? createdTo,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            return Ok(await _payments.ListAsync(
                User.MerchantId(), status, createdFrom, createdTo, page, pageSize, ct));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/capture")]
    public Task<IActionResult> Capture(Guid id, CancellationToken ct) =>
        Transition(m => _payments.CaptureAsync(m, id, ct));

    [HttpPost("{id:guid}/void")]
    public Task<IActionResult> Void(Guid id, CancellationToken ct) =>
        Transition(m => _payments.VoidAsync(m, id, ct));

    [HttpPost("{id:guid}/refund")]
    public Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest request, CancellationToken ct) =>
        Transition(m => _payments.RefundAsync(m, id, request.Amount, ct));

    private async Task<IActionResult> Transition(Func<Guid, Task<PaymentResponse?>> action)
    {
        try
        {
            var result = await action(User.MerchantId());
            return result is null ? NotFound() : Ok(result);
        }
        catch (DomainException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}
