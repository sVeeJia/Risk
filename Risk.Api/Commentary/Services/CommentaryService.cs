using System.Globalization;
using Risk.Api.DataStructures;
using Risk.Api.Models;

namespace Risk.Api.Commentary;

public interface ICommentaryService
{
    // Adds one Commentary event to the log for every dice roll the human player made in the events the log holds.
    Task AddAsync(Game game, EventLog log, CancellationToken ct = default);
}

public sealed class CommentaryService : ICommentaryService
{
    private readonly ICommentaryGenerator _generator;

    public CommentaryService(ICommentaryGenerator generator) => _generator = generator;

    public async Task AddAsync(Game game, EventLog log, CancellationToken ct = default)
    {
        // Computer players get no commentary: it saves calls to the language model.
        var rounds = RoundFactsBuilder.Build(game, log.Events.ToList())
            .Where(round => round.AttackerIsHuman)
            .ToList();

        if (rounds.Count == 0)
        {
            return;
        }

        var lines = await Task.WhenAll(rounds.Select(round => _generator.GenerateAsync(round, ct)));

        for (var i = 0; i < rounds.Count; i++)
        {
            log.Append(
                EventType.Commentary,
                string.IsNullOrEmpty(rounds[i].AttackerPlayerId) ? null : rounds[i].AttackerPlayerId,
                lines[i].Text,
                data: new Dictionary<string, string>
                {
                    // Which roll this line belongs to, so the screen can show it when that roll appears.
                    ["forSequence"] = rounds[i].Sequence.ToString(CultureInfo.InvariantCulture),
                    ["source"] = lines[i].Source,
                    ["fromCountryId"] = rounds[i].AttackerCountryId,
                    ["toCountryId"] = rounds[i].DefenderCountryId
                });
        }
    }
}