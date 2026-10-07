namespace MissionSplat.App;

using MissionSplat.Rules;

public sealed class SessionResult
{
    private SessionResult(SessionRejection? rejection, IReadOnlyList<GameEvent> events)
    {
        Rejection = rejection;
        Events = events;
    }

    public SessionRejection? Rejection { get; }

    public bool IsAccepted => Rejection is null;

    public IReadOnlyList<GameEvent> Events { get; }

    public static SessionResult Accept(IReadOnlyList<GameEvent> events)
    {
        if (events is null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        return new SessionResult(null, Copy(events));
    }

    public static SessionResult Reject(SessionRejection rejection)
    {
        if (rejection is null)
        {
            throw new ArgumentNullException(nameof(rejection));
        }

        return new SessionResult(rejection, []);
    }

    public static SessionResult Reject(Rejection rejection)
    {
        if (rejection is null)
        {
            throw new ArgumentNullException(nameof(rejection));
        }

        return Reject(new SessionRejection(rejection.Message, rejection));
    }

    private static GameEvent[] Copy(IReadOnlyList<GameEvent> events)
    {
        var copy = new GameEvent[events.Count];
        for (var i = 0; i < events.Count; i++)
        {
            copy[i] = events[i];
        }

        return copy;
    }
}

public sealed record SessionRejection(string Message, Rejection? RulesRejection);
