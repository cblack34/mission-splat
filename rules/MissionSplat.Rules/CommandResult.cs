namespace MissionSplat.Rules;

public sealed class CommandResult
{
    private CommandResult(Game? game, Rejection? rejection, IReadOnlyList<GameEvent> events)
    {
        Game = game;
        Rejection = rejection;
        Events = events;
    }

    public Game? Game { get; }

    public Rejection? Rejection { get; }

    public bool IsAccepted => Rejection is null;

    public IReadOnlyList<GameEvent> Events { get; }

    public static CommandResult Accept(Game game, IReadOnlyList<GameEvent> events)
    {
        if (game is null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        return new CommandResult(game, null, events);
    }

    public static CommandResult Reject(Game? game, Rejection rejection)
    {
        if (rejection is null)
        {
            throw new ArgumentNullException(nameof(rejection));
        }

        return new CommandResult(game, rejection, []);
    }
}
