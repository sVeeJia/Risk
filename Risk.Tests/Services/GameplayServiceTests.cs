using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.Models;
using Risk.Api.Services;
using Risk.Api.Services.Interfaces;
using Risk.Tests.Fakes;
using Risk.Tests.Helpers;

namespace Risk.Tests.Services;

public class GameplayServiceTests
{
    private readonly FakeGameRepository _games = new();
    private readonly FakeGameEventRepository _events = new();
    private readonly GameplayService _service;

    public GameplayServiceTests() => _service = new GameplayService(_games, _events);

    // A saved game owned by user-1 where the human plays first.
    private Game SavedGame(int humanSoldiers = 5, int enemySoldiers = 3)
    {
        var game = TestGames.HumanFirst(humanSoldiers, enemySoldiers);
        _games.Items.Add(game);
        return game;
    }

    private Task<GameplayResult> Attack(Game game, string userId = "user-1") =>
        _service.StartAttackAsync(userId, game.Id, game.Human().StartingCountryId, game.EnemyCountry());

    [Fact]
    public async Task StartAttack_AsTheOwner_SavesTheGameAndAppendsTheEvent()
    {
        var game = SavedGame();

        var result = await Attack(game);

        Assert.Equal(GameplayStatus.Success, result.Status);
        Assert.NotNull(game.ActiveAttack);
        Assert.Equal(1, game.Version);
        var saved = Assert.Single(_events.Items);
        Assert.Equal(EventType.AttackStarted, saved.Type);
        Assert.Equal(1, saved.Sequence);
    }

    [Fact]
    public async Task NewEvents_ContinueAfterTheExistingSequence()
    {
        var game = SavedGame();
        _events.Items.Add(new GameEvent { Id = "e1", GameId = game.Id, Sequence = 1, Message = "start" });
        _events.Items.Add(new GameEvent { Id = "e2", GameId = game.Id, Sequence = 2, Message = "turn" });

        await Attack(game);

        Assert.Equal(3, _events.Items.Max(e => e.Sequence));
    }

    [Fact]
    public async Task AnotherUsersGame_IsNotFound_AndNothingChanges()
    {
        var game = SavedGame();

        var result = await Attack(game, userId: "user-2");

        Assert.Equal(GameplayStatus.NotFound, result.Status);
        Assert.Null(game.ActiveAttack);
        Assert.Empty(_events.Items);
    }

    [Fact]
    public async Task UnknownGame_IsNotFound()
    {
        var result = await _service.RollAsync("user-1", Guid.NewGuid().ToString());

        Assert.Equal(GameplayStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task BrokenRule_IsRejected_AndNothingIsSaved()
    {
        var game = SavedGame();

        // Attacking from the enemy's country: the human doesn't own it.
        var result = await _service.StartAttackAsync("user-1", game.Id, game.EnemyCountry(), game.Human().StartingCountryId);

        Assert.Equal(GameplayStatus.Rejected, result.Status);
        Assert.Equal("You can only attack from a country you own.", result.Error);
        Assert.Equal(0, game.Version);
        Assert.Empty(_events.Items);
    }

    [Fact]
    public async Task VersionCollision_ReturnsConflict_AndWritesNoEvents()
    {
        var game = SavedGame();
        _games.FailNextUpdate = true;

        var result = await Attack(game);

        Assert.Equal(GameplayStatus.Conflict, result.Status);
        Assert.Empty(_events.Items);
    }

    [Fact]
    public async Task Roll_AfterAnAttackStarts_AppendsABattleRound()
    {
        var game = SavedGame();
        await Attack(game);

        var result = await _service.RollAsync("user-1", game.Id);

        Assert.Equal(GameplayStatus.Success, result.Status);
        Assert.Equal(new[] { EventType.AttackStarted, EventType.BattleRound }, _events.Items.Select(e => e.Type));
        Assert.Equal(new[] { 1, 2 }, _events.Items.Select(e => e.Sequence));
    }

    [Fact]
    public async Task Stop_EndsTheAttack()
    {
        var game = SavedGame();
        await Attack(game);

        var result = await _service.StopAttackAsync("user-1", game.Id);

        Assert.Equal(GameplayStatus.Success, result.Status);
        Assert.Null(game.ActiveAttack);
        Assert.Equal(GamePhase.Action, game.Phase);
    }

    [Fact]
    public async Task Transfer_AfterAConquest_MovesTheSoldiersAndEndsTheAttack()
    {
        var game = SavedGame(humanSoldiers: 5, enemySoldiers: 1);
        var human = game.Human();
        new StartAttackCommand(human.Id, human.StartingCountryId, game.EnemyCountry())
            .Execute(game, new EventLog(game.Id));
        new RollAttackCommand(human.Id, new FakeScriptedRandomDiceRolls(6, 1))
            .Execute(game, new EventLog(game.Id));
        Assert.Equal(GamePhase.ConquestTransfer, game.Phase);

        var result = await _service.TransferAfterConquestAsync("user-1", game.Id, 2);

        Assert.Equal(GameplayStatus.Success, result.Status);
        Assert.Equal(2, game.Territories[game.EnemyCountry()].Soldiers);
        Assert.Equal(GamePhase.Action, game.Phase);
        Assert.Equal(new[] { EventType.Transfer, EventType.AttackEnded }, _events.Items.Select(e => e.Type));
    }
}