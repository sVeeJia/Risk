using System.Net;
using System.Net.Http.Json;

namespace Risk.Tests.Api;

public class AuthApiTests
{
    private static object NewAccount(string email = "a@example.com", string password = ApiTestApp.Password, string name = "Tester") =>
        new { email, password, displayName = name };

    [Fact]
    public async Task Register_CreatesTheAccount_AndSetsASafeCookie()
    {
        using var app = new ApiTestApp();
        var client = app.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewAccount());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
        Assert.StartsWith("risk_auth=", cookie);
        Assert.Contains("httponly", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("samesite=strict", cookie);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("not-an-email", ApiTestApp.Password, "Tester")]
    [InlineData("a@example.com", "short", "Tester")]
    [InlineData("a@example.com", ApiTestApp.Password, "")]
    [InlineData("a@example.com", ApiTestApp.Password, "x")]
    public async Task Register_WithInvalidInput_Returns400(string email, string password, string name)
    {
        using var app = new ApiTestApp();
        var client = app.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewAccount(email, password, name));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_TheSameEmailTwice_Returns409_EvenInADifferentCase()
    {
        using var app = new ApiTestApp();
        var client = app.CreateApiClient();

        await client.PostAsJsonAsync("/api/auth/register", NewAccount("a@example.com"));
        var second = await client.PostAsJsonAsync("/api/auth/register", NewAccount("A@EXAMPLE.COM"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_WithAWrongPasswordOrAnUnknownEmail_LooksTheSame()
    {
        using var app = new ApiTestApp();
        var client = app.CreateApiClient();
        await client.PostAsJsonAsync("/api/auth/register", NewAccount("a@example.com"));

        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new { email = "a@example.com", password = "wrong-password-123" });
        var unknownEmail = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.com", password = "wrong-password-123" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(await wrongPassword.Content.ReadAsStringAsync(), await unknownEmail.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_ThenMe_ThenLogout_ThenMeIsRefused()
    {
        using var app = new ApiTestApp();
        await app.CreateApiClient().PostAsJsonAsync("/api/auth/register", NewAccount("a@example.com"));

        var browser = app.CreateApiClient();
        var login = await browser.PostAsJsonAsync("/api/auth/login", new { email = "a@example.com", password = ApiTestApp.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var me = await browser.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Contains("a@example.com", await me.Content.ReadAsStringAsync());

        var logout = await browser.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Me_WithoutALogin_Returns401()
    {
        using var app = new ApiTestApp();

        var response = await app.CreateApiClient().GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_IsLimitedToTenAttemptsAMinute()
    {
        using var app = new ApiTestApp();
        var client = app.CreateApiClient();

        for (var i = 0; i < 10; i++)
        {
            var attempt = await client.PostAsJsonAsync("/api/auth/login", new { email = "a@example.com", password = "wrong-password-123" });
            Assert.Equal(HttpStatusCode.Unauthorized, attempt.StatusCode);
        }

        var eleventh = await client.PostAsJsonAsync("/api/auth/login", new { email = "a@example.com", password = "wrong-password-123" });

        Assert.Equal(HttpStatusCode.TooManyRequests, eleventh.StatusCode);
    }
}