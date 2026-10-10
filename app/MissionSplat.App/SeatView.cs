namespace MissionSplat.App;

using MissionSplat.Rules;

public sealed class SeatView
{
    public SeatView(
        SeatId seat,
        IReadOnlyList<Mission> unclaimedMissions,
        IReadOnlyList<SeatClaims> claims,
        BoardView board,
        SeatId currentSeat,
        bool hasEnded,
        Tile? pendingMatchTile,
        IReadOnlyList<PowerCharge> remainingUses)
    {
        Seat = seat;
        UnclaimedMissions = Copied.Items(unclaimedMissions, nameof(unclaimedMissions));
        Claims = Copied.Items(claims, nameof(claims));
        Board = board ?? throw new ArgumentNullException(nameof(board));
        CurrentSeat = currentSeat;
        HasEnded = hasEnded;
        PendingMatchTile = pendingMatchTile;
        RemainingUses = Copied.List(remainingUses, nameof(remainingUses));
    }

    public SeatId Seat { get; }

    public IReadOnlyList<Mission> UnclaimedMissions { get; }

    public IReadOnlyList<SeatClaims> Claims { get; }

    public BoardView Board { get; }

    public SeatId CurrentSeat { get; }

    public bool HasEnded { get; }

    public Tile? PendingMatchTile { get; }

    // One entry per use power the setup lists, in setup order; zero once the drawn tile's cells of it are spent.
    public IReadOnlyList<PowerCharge> RemainingUses { get; }
}

public sealed class SeatClaims
{
    public SeatClaims(SeatId seat, IReadOnlyList<Mission> missions)
    {
        Seat = seat;
        Missions = Copied.Items(missions, nameof(missions));
    }

    public SeatId Seat { get; }

    public IReadOnlyList<Mission> Missions { get; }
}

public sealed class BoardView
{
    public BoardView(IReadOnlyList<OccupiedTile> tiles, IReadOnlyList<OccupiedCell> cells)
    {
        Tiles = Copied.List(tiles, nameof(tiles));
        Cells = Copied.List(cells, nameof(cells));
    }

    public IReadOnlyList<OccupiedTile> Tiles { get; }

    public IReadOnlyList<OccupiedCell> Cells { get; }
}

public readonly record struct OccupiedTile(TileId Id, int TileX, int TileY);

public readonly record struct OccupiedCell(int CellX, int CellY, Cell Value);

public readonly record struct PowerCharge(SymbolId Power, int Remaining);
