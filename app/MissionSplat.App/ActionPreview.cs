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

    public static ActionPreview Accept(IReadOnlyList<MissionId> claimedMissions) =>
        new(null, Copied.List(claimedMissions, nameof(claimedMissions)));

    public static ActionPreview Reject(SessionRejection rejection)
    {
        if (rejection is null)
        {
            throw new ArgumentNullException(nameof(rejection));
        }

        return new ActionPreview(rejection, []);
    }

    public static ActionPreview Reject(Rejection rejection) => Reject(SessionRejection.From(rejection));
}
