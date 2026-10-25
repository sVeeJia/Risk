using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Risk.Api.Models;
using Risk.Api.Services;
using Xunit;

namespace Risk.Tests.Services;

public class TokenServiceTests
{
    private static TokenService Create(string? key = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = key ?? new string('k', 40),
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiryMinutes"] = "15"
            })
            .Build();

        return new TokenService(config);
    }

    private static User NewUser() => new()
    {
        Email = "tester@example.com",
        DisplayName = "Tester",
        PasswordHash = "$2a$12$notARealHashJustForTheTest"
    };

    [Fact]
    public void CreateToken_ContainsTheExpectedClaimsIssuerAndAudience()
    {
        var user = NewUser();

        var result = Create().CreateToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal(user.Id, token.Subject);
        Assert.Equal("Tester", token.Claims.First(c => c.Type == "name").Value);
        Assert.Equal("tester@example.com", token.Claims.First(c => c.Type == "email").Value);
        Assert.Equal("test-issuer", token.Issuer);
        Assert.Contains("test-audience", token.Audiences);
    }

    [Fact]
    public void CreateToken_ExpiresAboutFifteenMinutesFromNow()
    {
        var result = Create().CreateToken(NewUser());

        Assert.InRange(result.ExpiresAt,
            DateTimeOffset.UtcNow.AddMinutes(14),
            DateTimeOffset.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void CreateToken_NeverContainsThePasswordHash()
    {
        var user = NewUser();

        var result = Create().CreateToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.DoesNotContain(token.Claims, c => c.Value == user.PasswordHash);
    }

    [Fact]
    public void Constructor_WithAShortKey_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Create("too-short"));
    }
}