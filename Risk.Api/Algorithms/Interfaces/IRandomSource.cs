namespace Risk.Api.Algorithms.Interfaces;

public interface IRandomSource
{
    // Returns an integer from 0 up to (but not including) maxExclusive.
    int Next(int maxExclusive);
}