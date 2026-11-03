using Risk.Api.Commentary;

namespace Risk.Tests.Fakes;

public sealed class FakeCommentaryGenerator : ICommentaryGenerator
{
    public CommentaryResult Result { get; set; } = new("What a battle!", "llm");
    public List<RoundFacts> Rounds { get; } = new();

    public Task<CommentaryResult> GenerateAsync(RoundFacts round, CancellationToken ct = default)
    {
        Rounds.Add(round);
        return Task.FromResult(Result);
    }
}