using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Repository.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(string id);
    Task CreateAsync(User user);
}