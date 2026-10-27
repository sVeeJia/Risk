using System.Security.Cryptography;
using Risk.Api.DesignPatterns.Factory.Interfaces;
using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Models;
using Risk.Api.Services.Interfaces;

namespace Risk.Api.Services;

public sealed class GameService : IGameService
{
    private readonly ICountryRepository _countries;
    private readonly IGameRepository _games;
    private readonly IGameFactory _factory;
    private readonly IConfiguration _config;

    public GameService(ICountryRepository countries, IGameRepository games, IGameFactory factory, IConfiguration config)
    {
        _countries = countries;
        _games = games;
        _factory = factory;
        _config = config;
    }

    public async Task<StartGameResult> StartGameAsync(
        string userId, string displayName, string chosenCountryId, CancellationToken ct = default)
    {
        // Game:PlayableCountries = the top N countries by population that can be played.
        // If the setting is missing, every country is playable.
        var limit = _config.GetValue("Game:PlayableCountries", int.MaxValue);
        var playableCountries = (await _countries.GetAllAsync(ct)).Where(c => c.Rank <= limit).ToList();

        var chosen = playableCountries.FirstOrDefault(c => c.Id.Equals(chosenCountryId, StringComparison.OrdinalIgnoreCase));
        
        if (chosen is null)
        {
            return new StartGameResult(StartGameStatus.InvalidCountry, null);
        }

        var seed = BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8));
        var game = _factory.Create(userId, displayName, chosen.Id, playableCountries, seed, DateTime.UtcNow);

        await _games.CreateAsync(game, ct);
        return new StartGameResult(StartGameStatus.Success, game);
    }

    public async Task<Game?> GetAsync(string userId, string gameId, CancellationToken ct = default)
    {
        var game = await _games.GetByIdAsync(gameId, ct);

        return game is not null && game.OwnerUserId == userId ? game : null;
    }

    public Task<IReadOnlyList<Game>> ListAsync(string userId, CancellationToken ct = default) => _games.ListByOwnerAsync(userId, ct);
}