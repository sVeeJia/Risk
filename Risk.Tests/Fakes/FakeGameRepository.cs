using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Models;

namespace Risk.Tests.Fakes;

public sealed class FakeGameRepository : IGameRepository
{
    public List<Game> Items { get; } = new();

    public Task CreateAsync(Game game, CancellationToken ct = default)
    {
        Items.Add(game);
        return Task.CompletedTask;
    }

    public Task<Game?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(g => g.Id == id));

    public Task<IReadOnlyList<Game>> ListByOwnerAsync(string ownerUserId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Game>>(
            Items.Where(g => g.OwnerUserId == ownerUserId).OrderByDescending(g => g.CreatedAt).ToList());

    public Task<bool> UpdateAsync(Game game, CancellationToken ct = default)
    {
        if (!Items.Any(g => g.Id == game.Id)) return Task.FromResult(false);
        game.Version++;
        return Task.FromResult(true);
    }
}