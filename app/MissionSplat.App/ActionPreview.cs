namespace MissionSplat.App;

using MissionSplat.Rules;

public sealed class ActionPreview
{
    private ActionPreview(SessionRejection? rejection, IReadOnlyList<MissionId> claimedMissions)
    {
        Rejection = rejection;
        ClaimedMissions = claimedMissions;
    }

    public SessionRejection? Rejection { get; }

    public bool IsAccepted => Rejection is null;

    public IReadOnlyList<MissionId> ClaimedMissions { get; }

    public static ActionPreview Accept(IReadOnlyList<MissionId> claimedMissions)
    {
        if (claimedMissions is null)
        {
            throw new ArgumentNullException(nameof(claimedMissions));
        }

        var copy = new MissionId[claimedMissions.Count];
        for (var i = 0; i < claimedMissions.Count; i++)
        {
            copy[i] = claimedMissions[i];
        }

        return new ActionPreview(null, copy);
    }

    public static ActionPreview Reject(SessionRejection rejection)
    {
        if (rejection is null)
        {
            throw new ArgumentNullException(nameof(rejection));
        }

        return new ActionPreview(rejection, []);
    }

    public static ActionPreview Reject(Rejection rejection)
    {
        if (rejection is null)
        {
            throw new ArgumentNullException(nameof(rejection));
        }

        return Reject(new SessionRejection(rejection.Message, rejection));
    }
}
