using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayApi.Api.Auth;
using PayApi.Application.Payments;

namespace PayApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("payments/{id:guid}/ledger")]
public class LedgerController : ControllerBase
{
    private readonly PaymentService _payments;

    public LedgerController(PaymentService payments) => _payments = payments;

    [HttpGet]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var entries = await _payments.GetLedgerAsync(User.MerchantId(), id, ct);
        return entries is null ? NotFound() : Ok(entries);
    }
}
