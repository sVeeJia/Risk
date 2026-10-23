using MongoDB.Driver;
using Risk.Api.Models;
using Risk.Api.DesignPatterns.Repository.Interfaces;

namespace Risk.Api.DesignPatterns.Repository;

public sealed class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public UserRepository(IMongoClient client, IConfiguration config)
    {
        var database = client.GetDatabase(config["Mongo:Database"] ?? "risk");
        _users = database.GetCollection<User>("users");

        // Two accounts can never share an email.
        var emailIndex = new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true });
        _users.Indexes.CreateOne(emailIndex);
    }

    public async Task<User?> GetByEmailAsync(string email) => await _users.Find(u => u.Email == email).FirstOrDefaultAsync();

    public async Task<User?> GetByIdAsync(string id) => await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task<bool> CreateAsync(User user)
    {
        try
        {
            await _users.InsertOneAsync(user);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false; // the unique email index rejected it
        }
    }
}