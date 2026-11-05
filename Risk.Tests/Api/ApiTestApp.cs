using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Tests.Fakes;

namespace Risk.Tests.Api;

// Runs the real API in memory. Every repository is replaced by an in-memory fake, so no test can reach MongoDB.
// Every test makes its own app, so the rate limiters start empty each time.
public sealed class ApiTestApp : WebApplicationFactory<Program>
{
    public const string Password = "correct-horse-battery";

    static ApiTestApp()
    {
        // Program.cs reads these while it starts, so they are set before any app is created.
        Environment.SetEnvironmentVariable("Mongo__ConnectionString", "mongodb://localhost:27017");
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-signing-key-that-is-long-enough-for-hs256");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "risk-api");
        Environment.SetEnvironmentVariable("Jwt__Audience", "risk-web");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Mistral__ApiKey", " "); // blank: no real calls to Mistral
    }

    public FakeUserRepository Users { get; } = new();
    public FakeGameRepository Games { get; } = new();
    public FakeGameEventRepository Events { get; } = new();
    public FakeCountryRepository Countries { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IGameRepository>();
            services.RemoveAll<IGameEventRepository>();
            services.RemoveAll<ICountryRepository>();

            services.AddSingleton<IUserRepository>(Users);
            services.AddSingleton<IGameRepository>(Games);
            services.AddSingleton<IGameEventRepository>(Events);
            services.AddSingleton<ICountryRepository>(Countries);
        });
    }

    // https, because the login cookie is marked Secure. Each client has its own cookies, like its own browser.
    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
        AllowAutoRedirect = false
    });

    // A client that has just registered a new account, so it is logged in.
    public async Task<HttpClient> SignedInClientAsync(string email = "player@example.com")
    {
        var client = CreateApiClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = Password, displayName = "Tester" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return client;
    }

    // Only valid once the app has started (after the first client is created).
    public string AnyCountryId() => Countries.Items.OrderBy(c => c.Rank).First().Id;

    public async Task<string> StartGameAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/games", new { startingCountryId = AnyCountryId() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetString()!;
    }
}