namespace MissionSplat.App;

using MissionSplat.Rules;

public sealed class PlacementPreview
{
    private PlacementPreview(SessionRejection? rejection, IReadOnlyList<MissionId> claimedMissions)
    {
        Rejection = rejection;
        ClaimedMissions = claimedMissions;
    }

    public SessionRejection? Rejection { get; }

    public bool IsAccepted => Rejection is null;

    public IReadOnlyList<MissionId> ClaimedMissions { get; }

    public static PlacementPreview Accept(IReadOnlyList<MissionId> claimedMissions)
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

        return new PlacementPreview(null, copy);
    }

    public static PlacementPreview Reject(SessionRejection rejection)
    {
        if (rejection is null)
        {
            throw new ArgumentNullException(nameof(rejection));
        }

        return new PlacementPreview(rejection, []);
    }

    public static PlacementPreview Reject(Rejection rejection)
    {
        if (rejection is null)
        {
            throw new ArgumentNullException(nameof(rejection));
        }

        return Reject(new SessionRejection(rejection.Message, rejection));
    }
}
