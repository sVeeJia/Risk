using Risk.Api.Dtos.Requests;
using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Models;
using Risk.Api.Services.Interfaces;

namespace Risk.Api.Services;

public sealed class AuthService : IAuthService
{
    private const int WorkFactor = 12;

    // Checked against when the email doesn't exist, so response time doesn't reveal which emails are registered.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("timing-protection-only", WorkFactor);

    private readonly IUserRepository _users;

    public AuthService(IUserRepository users)
    {
        _users = users;
    }

    public async Task<AuthResult> RegisterAsync(Register request)
    {
        var user = new User
        {
            Email = Normalize(request.Email),
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, WorkFactor)
        };

        var created = await _users.CreateAsync(user);
        return created? new AuthResult(AuthStatus.Success, user) : new AuthResult(AuthStatus.EmailTaken);
    }

    public async Task<AuthResult> LoginAsync(Login request)
    {
        var user = await _users.GetByEmailAsync(Normalize(request.Email));

        // Always run one BCrypt check, whether or not the user exists.
        var passwordMatches = BCrypt.Net.BCrypt.Verify(request.Password, user?.PasswordHash ?? DummyHash);

        return user is not null && passwordMatches? new AuthResult(AuthStatus.Success, user) : new AuthResult(AuthStatus.InvalidCredentials);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}