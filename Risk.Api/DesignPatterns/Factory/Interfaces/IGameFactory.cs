using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Factory.Interfaces;

public interface IGameFactory
{
    Game Create(
        string ownerUserId,
        string ownerDisplayName,
        string chosenCountryId,
        IReadOnlyList<Country> pool,
        long seed,
        DateTime nowUtc
    );
}