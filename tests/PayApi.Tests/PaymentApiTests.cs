using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayApi.Application.Merchants;
using PayApi.Infrastructure.Persistence;
using Xunit;

namespace PayApi.Tests;

public class PaymentApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public PaymentApiTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, Guid MerchantId)> NewMerchantClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<MerchantService>();
        var created = await service.CreateAsync("Test " + Guid.NewGuid(), CancellationToken.None);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", created.ApiKey);
        return (client, created.MerchantId);
    }

    private static HttpRequestMessage Authorize(string key, long amount = 5000)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/payments")
        {
            Content = JsonContent.Create(new { amount, currency = "usd" })
        };
        req.Headers.Add("Idempotency-Key", key);
        return req;
    }

    private static Guid ReadId(string json) =>
        JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();

    [Fact]
    public async Task Request_WithoutApiKey_Returns401()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.SendAsync(Authorize(Guid.NewGuid().ToString()));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithWrongApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "pk_test_wrong");
        var response = await client.SendAsync(Authorize(Guid.NewGuid().ToString()));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Merchant_CannotSeeAnotherMerchantsPayment()
    {
        var (clientA, _) = await NewMerchantClientAsync();
        var (clientB, _) = await NewMerchantClientAsync();

        var created = await clientA.SendAsync(Authorize(Guid.NewGuid().ToString()));
        var id = ReadId(await created.Content.ReadAsStringAsync());

        var asOwner = await clientA.GetAsync($"/payments/{id}");
        var asOther = await clientB.GetAsync($"/payments/{id}");

        Assert.Equal(HttpStatusCode.OK, asOwner.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, asOther.StatusCode);
    }

    [Fact]
    public async Task Authorize_SameKeyTwice_ReturnsSamePayment()
    {
        var (client, _) = await NewMerchantClientAsync();
        var key = Guid.NewGuid().ToString();

        var first = await client.SendAsync(Authorize(key));
        var second = await client.SendAsync(Authorize(key));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(ReadId(await first.Content.ReadAsStringAsync()),
                     ReadId(await second.Content.ReadAsStringAsync()));
        Assert.Equal("true", second.Headers.GetValues("Idempotent-Replayed").Single());
    }

    [Fact]
    public async Task Authorize_SameKeyDifferentBody_Returns422()
    {
        var (client, _) = await NewMerchantClientAsync();
        var key = Guid.NewGuid().ToString();

        await client.SendAsync(Authorize(key, 5000));
        var second = await client.SendAsync(Authorize(key, 9999));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
    }

    [Fact]
    public async Task Authorize_20ParallelRequestsSameKey_CreatesExactlyOnePayment()
    {
        var (client, merchantId) = await NewMerchantClientAsync();
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => client.SendAsync(Authorize(key))));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Payments.CountAsync(p => p.MerchantId == merchantId));
    }

    [Fact]
    public async Task Capture_50ParallelRequests_OnlyOneSucceeds_AndLedgerHasTwoEntries()
    {
        var (client, _) = await NewMerchantClientAsync();
        var created = await client.SendAsync(Authorize(Guid.NewGuid().ToString()));
        var id = ReadId(await created.Content.ReadAsStringAsync());

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 50).Select(_ => client.PostAsync($"/payments/{id}/capture", null)));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(49, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var ledger = await client.GetFromJsonAsync<JsonElement>($"/payments/{id}/ledger");
        Assert.Equal(2, ledger.GetArrayLength());
    }
}
