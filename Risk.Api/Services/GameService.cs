using System.Security.Cryptography;
using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Factory.Interfaces;
using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Models;
using Risk.Api.Services.Interfaces;

namespace Risk.Api.Services;

public sealed class GameService : IGameService
{
    private readonly ICountryRepository _countries;
    private readonly IGameRepository _games;
    private readonly IGameEventRepository _events;
    private readonly IGameFactory _factory;
    private readonly IConfiguration _config;

    public GameService(ICountryRepository countries, IGameRepository games, IGameEventRepository events, IGameFactory factory, IConfiguration config)
    {
        _countries = countries;
        _games = games;
        _events = events;
        _factory = factory;
        _config = config;
    }

    public async Task<StartGameResult> StartGameAsync(string userId, string displayName, string chosenCountryId, CancellationToken ct = default)
    {
        // Game:PlayableCountries = the top N countries by population that can be played. If the setting is missing, every country is playable.
        var limit = _config.GetValue("Game:PlayableCountries", int.MaxValue);
        var playableCountries = (await _countries.GetAllAsync(ct)).Where(c => c.Rank <= limit).ToList();

        var chosen = playableCountries.FirstOrDefault(c => c.Id.Equals(chosenCountryId, StringComparison.OrdinalIgnoreCase));
        if (chosen is null)
        {
            return new StartGameResult(StartGameStatus.InvalidCountry, null);
        }

        var seed = BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8));
        var game = _factory.Create(userId, displayName, chosen.Id, playableCountries, seed, DateTime.UtcNow);

        // A new game replaces any earlier game this user prepared but never played (and its events).
        var replacedGameIds = await _games.DeleteUnplayedAsync(userId, ct);
        await _events.DeleteByGamesAsync(replacedGameIds, ct);
        await _games.CreateAsync(game, ct);

        var firstPlayer = game.Players.Single(p => p.Id == game.TurnQueue.Peek());

        var log = new EventLog(game.Id);
        
        log.Append(
            EventType.GameStarted,
            null,
            $"Game started. {displayName} commands {chosen.Name}.",
            data: new Dictionary<string, string> { ["humanCountryId"] = chosen.Id });
        log.Append(
            EventType.TurnStarted,
            firstPlayer.Id,
            $"{firstPlayer.Name} plays first.",
            data: new Dictionary<string, string> { ["round"] = game.Round.ToString() });

        await _events.AppendAsync(log.Events, ct);

        return new StartGameResult(StartGameStatus.Success, game);
    }

    public async Task<Game?> GetAsync(string userId, string gameId, CancellationToken ct = default)
    {
        var game = await _games.GetByIdAsync(gameId, ct);
        return game is not null && game.OwnerUserId == userId ? game : null;
    }

    public Task<IReadOnlyList<Game>> ListAsync(string userId, CancellationToken ct = default) => _games.ListByOwnerAsync(userId, ct);

    public async Task<IReadOnlyList<GameEvent>?> GetEventsAsync(string userId, string gameId, int afterSequence, int limit, CancellationToken ct = default)
    {
        var game = await GetAsync(userId, gameId, ct);
        if (game is null) return null;

        return await _events.ListAsync(game.Id, afterSequence, limit, ct);
    }
}