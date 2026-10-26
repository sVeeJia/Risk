using Risk.Api.Models;

namespace Risk.Api.Services.Interfaces;

public sealed record TokenResult(string Token, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    TokenResult CreateToken(User user);
}