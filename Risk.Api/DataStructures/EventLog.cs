using Risk.Api.Models;

namespace Risk.Api.DataStructures;

// append-only event list. Events can be added and read, never edited or removed.
// Sequence numbers count up by one, so any client can ask "everything after event N".
public sealed class EventLog
{
    private readonly string _gameId;
    private readonly List<GameEvent> _events = new();
    private int _lastSequence;

    // Pass the last saved sequence number to continue an existing game's log.
    public EventLog(string gameId, int lastSequence = 0)
    {
        _gameId = gameId;
        _lastSequence = lastSequence;
    }

    // The events added through this log, in the order they happened.
    public IReadOnlyList<GameEvent> Events => _events;

    public GameEvent Append(EventType type, string? playerId, string message, string? justification = null, Dictionary<string, string>? data = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("An event needs a message.", nameof(message));
          
        }

        _lastSequence++;
        var gameEvent = new GameEvent
        {
            Id = Guid.NewGuid().ToString(),
            GameId = _gameId,
            Sequence = _lastSequence,
            Type = type,
            PlayerId = playerId,
            Message = message,
            Justification = justification,
            Data = data ?? new Dictionary<string, string>(),
            Timestamp = DateTime.UtcNow
        };

        _events.Add(gameEvent);
        return gameEvent;
    }

    // Everything that happened after the given sequence number.
    public IReadOnlyList<GameEvent> Since(int sequence) => _events.Where(e => e.Sequence > sequence).ToList();
}