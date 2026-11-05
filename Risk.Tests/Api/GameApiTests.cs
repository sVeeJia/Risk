using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Risk.Tests.Api;

public class GameApiTests
{
    private const string SomeGame = "3f2b8c1e-5d44-4a07-9c36-0a1b2c3d4e5f";

    [Theory]
    [InlineData("GET", "/api/games")]
    [InlineData("POST", "/api/games")]
    [InlineData("GET", "/api/games/" + SomeGame)]
    [InlineData("GET", "/api/games/" + SomeGame + "/events")]
    [InlineData("POST", "/api/games/" + SomeGame + "/attack/roll")]
    [InlineData("POST", "/api/games/" + SomeGame + "/end-turn")]
    [InlineData("POST", "/api/games/" + SomeGame + "/npc-turn")]
    public async Task WithoutALogin_EveryGameEndpointReturns401(string method, string url)
    {
        using var app = new ApiTestApp();
        var client = app.CreateApiClient();

        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("ZZ")]   // two letters, but not a country
    [InlineData("123")]
    [InlineData("ABC")]
    [InlineData("")]
    public async Task StartingAGame_WithAnInvalidCountry_Returns400(string country)
    {
        using var app = new ApiTestApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/games", new { startingCountryId = country });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StartingAGame_Returns201_AndTheResponseHoldsNoSecrets()
    {
        using var app = new ApiTestApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/games", new { startingCountryId = app.AnyCountryId() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var text = await response.Content.ReadAsStringAsync();
        foreach (var secret in new[] { "ownerUserId", "seed", "rng", "passwordHash" })
        {
            Assert.DoesNotContain(secret, text, StringComparison.OrdinalIgnoreCase);
        }

        using var json = JsonDocument.Parse(text);
        Assert.Equal(10, json.RootElement.GetProperty("territories").GetArrayLength());
        Assert.Equal(10, json.RootElement.GetProperty("players").GetArrayLength());
    }

    [Fact]
    public async Task AnotherPlayer_CannotSeeOrPlayYourGame()
    {
        using var app = new ApiTestApp();
        var owner = await app.SignedInClientAsync("owner@example.com");
        var gameId = await app.StartGameAsync(owner);
        var other = await app.SignedInClientAsync("other@example.com");

        // The owner can see it, so the 404s below are about ownership and not a broken route.
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/games/{gameId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/games/{gameId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/games/{gameId}/events")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"/api/games/{gameId}/attack/roll", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"/api/games/{gameId}/end-turn", null)).StatusCode);

        var list = await other.GetAsync("/api/games");
        Assert.Equal("[]", await list.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AGameIdThatIsNotAGuid_Returns404()
    {
        using var app = new ApiTestApp();
        var client = await app.SignedInClientAsync();

        var response = await client.GetAsync("/api/games/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("limit=0")]
    [InlineData("limit=501")]
    [InlineData("after=-1")]
    public async Task EventPaging_OutsideTheAllowedRange_Returns400(string query)
    {
        using var app = new ApiTestApp();
        var client = await app.SignedInClientAsync();

        var response = await client.GetAsync($"/api/games/{SomeGame}/events?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RollingWithoutAnAttackInProgress_IsRefusedByTheServer()
    {
        using var app = new ApiTestApp();
        var client = await app.SignedInClientAsync();
        var gameId = await app.StartGameAsync(client);

        var response = await client.PostAsync($"/api/games/{gameId}/attack/roll", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AttackingBetweenCountriesThatDoNotExist_IsRefusedByTheServer()
    {
        using var app = new ApiTestApp();
        var client = await app.SignedInClientAsync();
        var gameId = await app.StartGameAsync(client);

        var response = await client.PostAsJsonAsync($"/api/games/{gameId}/attack", new { fromCountryId = "ZZ", toCountryId = "QQ" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}