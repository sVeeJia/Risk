using Risk.Api.DataStructures;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Command.Interfaces;

public interface IGameCommand
{
    // Checks the rules and applies the changes to the game, writing events to the log.
    // On failure nothing is changed and no events are written.
    CommandResult Execute(Game game, EventLog log);
}