namespace Risk.Api.Commentary;

// The sentence
public sealed record CommentaryResult(string Text, string Source);

public interface ICommentaryGenerator
{
    // Always returns a sentence.
    Task<CommentaryResult> GenerateAsync(RoundFacts round, CancellationToken ct = default);
}