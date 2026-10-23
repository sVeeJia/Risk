using Risk.Api.Dtos.Requests;
using Risk.Api.Services;
using Risk.Api.Services.Interfaces;
using Risk.Tests.Fakes;
using Xunit;

namespace Risk.Tests.Services;

public class AuthServiceTests
{
    private const string Password = "Test1234567!";

    private static AuthService CreateService() => new(new FakeUserRepository());

    private static Register NewRegistration(string email = "tester@example.com") => new()
    {
        Email = email,
        Password = Password,
        DisplayName = "Tester"
    };

    [Fact]
    public async Task Register_StoresBcryptHash_NotThePlainPassword()
    {
        var result = await CreateService().RegisterAsync(NewRegistration());

        Assert.True(result.Succeeded);
        Assert.NotEqual(Password, result.User!.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(Password, result.User.PasswordHash));
    }

    [Fact]
    public async Task Register_TrimsAndLowercasesTheEmail()
    {
        var result = await CreateService().RegisterAsync(NewRegistration("  Tester@Example.COM "));

        Assert.Equal("tester@example.com", result.User!.Email);
    }

    [Fact]
    public async Task Register_SameEmailInDifferentCasing_ReturnsEmailTaken()
    {
        var service = CreateService();
        await service.RegisterAsync(NewRegistration("tester@example.com"));

        var second = await service.RegisterAsync(NewRegistration("TESTER@example.com"));

        Assert.Equal(AuthStatus.EmailTaken, second.Status);
        Assert.Null(second.User);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Succeeds()
    {
        var service = CreateService();
        await service.RegisterAsync(NewRegistration());

        var result = await service.LoginAsync(new Login { Email = "tester@example.com", Password = Password });

        Assert.True(result.Succeeded);
        Assert.Equal("tester@example.com", result.User!.Email);
    }

    [Fact]
    public async Task Login_WithEmailInDifferentCasing_Succeeds()
    {
        var service = CreateService();
        await service.RegisterAsync(NewRegistration());

        var result = await service.LoginAsync(new Login { Email = "TESTER@EXAMPLE.COM", Password = Password });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsInvalidCredentials()
    {
        var service = CreateService();
        await service.RegisterAsync(NewRegistration());

        var result = await service.LoginAsync(new Login { Email = "tester@example.com", Password = "WrongPassword123" });

        Assert.Equal(AuthStatus.InvalidCredentials, result.Status);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsTheSameFailureAsAWrongPassword()
    {
        var service = CreateService();
        await service.RegisterAsync(NewRegistration());

        var wrongPassword = await service.LoginAsync(new Login { Email = "tester@example.com", Password = "WrongPassword123" });
        var unknownEmail = await service.LoginAsync(new Login { Email = "nobody@example.com", Password = Password });

        Assert.Equal(wrongPassword.Status, unknownEmail.Status);
        Assert.Null(unknownEmail.User);
    }
}