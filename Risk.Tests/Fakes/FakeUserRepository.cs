using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Models;

namespace Risk.Tests.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public Task<User?> GetByEmailAsync(string email) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Email == email));

    public Task<User?> GetByIdAsync(string id) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<bool> CreateAsync(User user)
    {
        // Mimics the unique email index in MongoDB.
        if (_users.Any(u => u.Email == user.Email))
            return Task.FromResult(false);

        _users.Add(user);
        return Task.FromResult(true);
    }
}