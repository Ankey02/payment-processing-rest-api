using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using PayApi.Application.Merchants;
using PayApi.Domain.Common;

namespace PayApi.Api.Controllers;

[ApiController]
[Route("admin/merchants")]
public class AdminController : ControllerBase
{
    private readonly MerchantService _merchants;
    private readonly IConfiguration _config;

    public AdminController(MerchantService merchants, IConfiguration config)
    {
        _merchants = merchants;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMerchantRequest request,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey,
        CancellationToken ct)
    {
        var expected = _config["Admin:Key"];
        if (string.IsNullOrEmpty(expected)) return NotFound(); // endpoint is off unless configured

        var supplied = Encoding.UTF8.GetBytes(adminKey ?? "");
        if (!CryptographicOperations.FixedTimeEquals(supplied, Encoding.UTF8.GetBytes(expected)))
            return Unauthorized();

        try
        {
            return StatusCode(201, await _merchants.CreateAsync(request.Name, ct));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
