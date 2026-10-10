namespace MissionSplat.App;

using MissionSplat.Rules;

public sealed class AiPlayer : IPlayer
{
    private readonly Func<int, IReadOnlyList<LegalPlacement>> _legalPlacements;
    private readonly Func<GameAction, ActionPreview> _preview;

    public AiPlayer(
        SeatId seat,
        Func<int, IReadOnlyList<LegalPlacement>> legalPlacements,
        Func<GameAction, ActionPreview> preview)
    {
        _legalPlacements = legalPlacements ?? throw new ArgumentNullException(nameof(legalPlacements));
        _preview = preview ?? throw new ArgumentNullException(nameof(preview));
        Seat = seat;
    }

    public SeatId Seat { get; }

    // It only places beside: a power use or a stack is never chosen.
    public GameAction ChooseAction(SeatView view)
    {
        if (view is null)
        {
            throw new ArgumentNullException(nameof(view));
        }

        if (!view.Seat.Equals(Seat))
        {
            throw new ArgumentException("The view is for a different seat.", nameof(view));
        }

        var ownMissions = IdsOf(view.UnclaimedMissions);
        Place? firstAccepted = null;
        for (var quarterTurns = 0; quarterTurns < 4; quarterTurns++)
        {
            foreach (var legal in _legalPlacements(quarterTurns))
            {
                if (legal.Kind != PlacementKind.Beside)
                {
                    continue;
                }

                var place = new Place(legal.TileX, legal.TileY, quarterTurns);
                var preview = _preview(place);
                if (!preview.IsAccepted)
                {
                    continue;
                }

                if (ClaimsOwnMission(preview.ClaimedMissions, ownMissions))
                {
                    return place;
                }

                firstAccepted ??= place;
            }
        }

        if (firstAccepted is not null)
        {
            return firstAccepted;
        }

        var pending = view.PendingMatchTile;
        var message = pending is null
            ? "No accepted placement was found."
            : "No accepted placement was found for tile '" + pending.Id.Value + "'.";
        throw new InvalidOperationException(message);
    }

    private static bool ClaimsOwnMission(IReadOnlyList<MissionId> claimed, HashSet<MissionId> ownMissions)
    {
        for (var i = 0; i < claimed.Count; i++)
        {
            if (ownMissions.Contains(claimed[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<MissionId> IdsOf(IReadOnlyList<Mission> missions)
    {
        var ids = new HashSet<MissionId>();
        for (var i = 0; i < missions.Count; i++)
        {
            ids.Add(missions[i].Id);
        }

        return ids;
    }
}
