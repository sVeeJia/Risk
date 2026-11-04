using Risk.Api.Commentary;
using Risk.Api.DataStructures;
using Risk.Api.Models;

namespace Risk.Tests.Fakes;

public sealed class FakeCommentaryService : ICommentaryService
{
    public int Calls { get; private set; }
    public bool Throw { get; set; }

    public Task AddAsync(Game game, EventLog log, CancellationToken ct = default)
    {
        Calls++;
        if (Throw) throw new InvalidOperationException("The commentary failed.");

        log.Append(EventType.Commentary, null, "Fake commentary.");
        return Task.CompletedTask;
    }
}