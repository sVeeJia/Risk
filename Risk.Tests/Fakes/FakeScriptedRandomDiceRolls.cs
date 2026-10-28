using Risk.Api.Algorithms.Interfaces;

namespace Risk.Tests.Fakes;

// Returns the dice faces it was given, in order, so tests can control every roll.
public sealed class FakeScriptedRandomDiceRolls : IRandomSource
{
    private readonly Queue<int> _faces;

    public FakeScriptedRandomDiceRolls(params int[] faces) => _faces = new Queue<int>(faces);

    public int Next(int maxExclusive)
    {
        if (maxExclusive != 6) throw new InvalidOperationException("FakeScriptedRandomDiceRolls only scripts dice.");
        return _faces.Dequeue() - 1;
    }
}