using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Risk.Tests.Api;

public class SecurityApiTests
{
    private const string SomeGame = "3f2b8c1e-5d44-4a07-9c36-0a1b2c3d4e5f";

    private static string Header(HttpResponseMessage response, string name) => Assert.Single(response.Headers.GetValues(name));

    [Fact]
    public async Task EveryResponse_CarriesTheSecurityHeaders()
    {
        using var app = new ApiTestApp();

        var response = await app.CreateApiClient().GetAsync("/api/auth/me");

        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        Assert.Contains("default-src 'self'", Header(response, "Content-Security-Policy"));
    }

    [Fact]
    public async Task APublicLinkShowsTheGameToAnyone_WithoutSecrets()
    {
        using var app = new ApiTestApp();
        var owner = await app.SignedInClientAsync("owner@example.com");
        var gameId = await app.StartGameAsync(owner);
        var visitor = app.CreateApiClient(); // not logged in

        var game = await visitor.GetAsync($"/api/public/games/{gameId}");
        var events = await visitor.GetAsync($"/api/public/games/{gameId}/events");

        Assert.Equal(HttpStatusCode.OK, game.StatusCode);
        Assert.Equal(HttpStatusCode.OK, events.StatusCode);
        Assert.True(game.Headers.CacheControl?.NoStore);

        var text = await game.Content.ReadAsStringAsync();
        foreach (var secret in new[] { "ownerUserId", "seed", "rng" })
        {
            Assert.DoesNotContain(secret, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("/api/public/games/" + SomeGame, 404)]
    [InlineData("/api/public/games/" + SomeGame + "/events", 404)]
    [InlineData("/api/public/games/not-a-guid", 404)]
    [InlineData("/api/public/games/" + SomeGame + "/events?limit=501", 400)]
    public async Task PublicEndpoints_RefuseUnknownGamesAndBadPaging(string url, int expected)
    {
        using var app = new ApiTestApp();

        var response = await app.CreateApiClient().GetAsync(url);

        Assert.Equal(expected, (int)response.StatusCode);
    }

    [Fact]
    public async Task APublicLink_CannotChangeAnything()
    {
        using var app = new ApiTestApp();
        var owner = await app.SignedInClientAsync("owner@example.com");
        var gameId = await app.StartGameAsync(owner);

        var response = await app.CreateApiClient().PostAsJsonAsync($"/api/public/games/{gameId}", new { });

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task GameEndpoints_AreLimitedPerPlayer()
    {
        using var app = new ApiTestApp();
        var busy = await app.SignedInClientAsync("busy@example.com");
        var calm = await app.SignedInClientAsync("calm@example.com");

        for (var i = 0; i < 120; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await busy.GetAsync("/api/games")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await busy.GetAsync("/api/games")).StatusCode);

        // Another player has their own budget.
        Assert.Equal(HttpStatusCode.OK, (await calm.GetAsync("/api/games")).StatusCode);
    }

    [Fact]
    public async Task PublicEndpoints_AreLimitedPerAddress()
    {
        using var app = new ApiTestApp();
        var visitor = app.CreateApiClient();

        for (var i = 0; i < 120; i++)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/public/games/{SomeGame}")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await visitor.GetAsync($"/api/public/games/{SomeGame}")).StatusCode);
    }
}