using Microsoft.AspNetCore.Mvc;
using PayApi.Application.Payments;

namespace PayApi.Api.Controllers;

[ApiController]
[Route("payments/{id:guid}/ledger")]
public class LedgerController : ControllerBase
{
    private readonly PaymentService _payments;

    public LedgerController(PaymentService payments) => _payments = payments;

    [HttpGet]
    public async Task<IActionResult> Get(
        Guid id, [FromHeader(Name = "X-Merchant-Id")] Guid? merchantId, CancellationToken ct)
    {
        if (merchantId is null)
            return BadRequest(new { error = "X-Merchant-Id header is required." });
        var entries = await _payments.GetLedgerAsync(merchantId.Value, id, ct);
        return entries is null ? NotFound() : Ok(entries);
    }
}
