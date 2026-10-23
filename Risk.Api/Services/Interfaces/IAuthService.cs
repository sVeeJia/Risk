using Risk.Api.Dtos.Requests;
using Risk.Api.Models;

namespace Risk.Api.Services.Interfaces;

public enum AuthStatus
{
    Success,
    EmailTaken,
    InvalidCredentials
}

public sealed record AuthResult(AuthStatus Status, User? User = null)
{
    public bool Succeeded => Status == AuthStatus.Success;
}

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(Register request);
    Task<AuthResult> LoginAsync(Login request);
}