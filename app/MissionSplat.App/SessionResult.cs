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

    public static SessionResult Accept(IReadOnlyList<GameEvent> events) =>
        new(null, Copied.List(events, nameof(events)));

    public static SessionResult Reject(SessionRejection rejection)
    {
        if (rejection is null)
        {
            throw new ArgumentNullException(nameof(rejection));
        }

        return new SessionResult(rejection, []);
    }

    public static SessionResult Reject(Rejection rejection) => Reject(SessionRejection.From(rejection));
}

public sealed record SessionRejection(string Message, Rejection? RulesRejection)
{
    public static SessionRejection From(Rejection rejection)
    {
        if (rejection is null)
        {
            throw new ArgumentNullException(nameof(rejection));
        }

        return new SessionRejection(rejection.Message, rejection);
    }
}
