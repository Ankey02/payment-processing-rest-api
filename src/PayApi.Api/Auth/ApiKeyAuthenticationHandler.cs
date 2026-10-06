using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PayApi.Application.Merchants;

namespace PayApi.Api.Auth;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    private readonly MerchantService _merchants;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        MerchantService merchants) : base(options, logger, encoder)
    {
        _merchants = merchants;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var key = Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(key))
            return AuthenticateResult.NoResult();

        var merchantId = await _merchants.FindMerchantIdAsync(key, Context.RequestAborted);
        if (merchantId is null)
            return AuthenticateResult.Fail("Invalid API key.");

        var identity = new ClaimsIdentity(
            new[] { new Claim("merchant_id", merchantId.Value.ToString()) }, SchemeName);
        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}

public static class ClaimsExtensions
{
    public static Guid MerchantId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirst("merchant_id")!.Value);
}
