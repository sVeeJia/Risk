using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Repository.Interfaces;

public interface IGameRepository
{
    Task CreateAsync(Game game, CancellationToken ct = default);
    Task<Game?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> ListByOwnerAsync(string ownerUserId, CancellationToken ct = default);

    // Saves the game only if nobody else changed it since it was loaded.
    // Returns false when the version no longer matches.
    Task<bool> UpdateAsync(Game game, CancellationToken ct = default);
    
    // Deletes the owner's games that nobody has acted on yet (Version is still 0).
    // Returns the ids of the games that were deleted.
    Task<IReadOnlyList<string>> DeleteUnplayedAsync(string ownerUserId, CancellationToken ct = default);
}