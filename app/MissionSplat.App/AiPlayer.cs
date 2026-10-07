namespace MissionSplat.App;

using MissionSplat.Rules;

public sealed class AiPlayer : IPlayer
{
    private static readonly (int X, int Y)[] NeighborSteps =
    [
        (1, 0),
        (-1, 0),
        (0, 1),
        (0, -1),
    ];

    private readonly Func<Placement, PlacementPreview> _preview;

    public AiPlayer(SeatId seat, Func<Placement, PlacementPreview> preview)
    {
        if (preview is null)
        {
            throw new ArgumentNullException(nameof(preview));
        }

        Seat = seat;
        _preview = preview;
    }

    public SeatId Seat { get; }

    public Placement ChoosePlacement(SeatView view)
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
        Placement? firstAccepted = null;
        foreach (var placement in OrthogonalPlacements(view.Board))
        {
            var preview = _preview(placement);
            if (!preview.IsAccepted)
            {
                continue;
            }

            if (ClaimsOwnMission(preview.ClaimedMissions, ownMissions))
            {
                return placement;
            }

            firstAccepted ??= placement;
        }

        if (firstAccepted is Placement chosen)
        {
            return chosen;
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

    private static IEnumerable<Placement> OrthogonalPlacements(BoardView board)
    {
        var seen = new HashSet<(int X, int Y)>();
        var neighbors = new List<(int X, int Y)>();
        foreach (var tile in board.Tiles)
        {
            foreach (var step in NeighborSteps)
            {
                var tileX = tile.TileX + step.X;
                var tileY = tile.TileY + step.Y;
                if (seen.Add((tileX, tileY)))
                {
                    neighbors.Add((tileX, tileY));
                }
            }
        }

        neighbors.Sort(static (left, right) =>
        {
            var byX = left.X.CompareTo(right.X);
            return byX != 0 ? byX : left.Y.CompareTo(right.Y);
        });

        foreach (var (tileX, tileY) in neighbors)
        {
            for (var quarterTurns = 0; quarterTurns < 4; quarterTurns++)
            {
                yield return new Placement(tileX, tileY, quarterTurns);
            }
        }
    }
}
