namespace Risk.Api.Realtime;

public static class GameHubGroups
{
    // One group per game: everyone watching that game is in it.
    public static string For(string gameId) => $"game:{gameId.ToLowerInvariant()}";
}