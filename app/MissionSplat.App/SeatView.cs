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
        Tile? pendingMatchTile)
    {
        Seat = seat;
        UnclaimedMissions = Copy(unclaimedMissions, nameof(unclaimedMissions));
        Claims = Copy(claims, nameof(claims));
        Board = board ?? throw new ArgumentNullException(nameof(board));
        CurrentSeat = currentSeat;
        HasEnded = hasEnded;
        PendingMatchTile = pendingMatchTile;
    }

    public SeatId Seat { get; }

    public IReadOnlyList<Mission> UnclaimedMissions { get; }

    public IReadOnlyList<SeatClaims> Claims { get; }

    public BoardView Board { get; }

    public SeatId CurrentSeat { get; }

    public bool HasEnded { get; }

    public Tile? PendingMatchTile { get; }

    private static T[] Copy<T>(IReadOnlyList<T>? items, string name)
        where T : class
    {
        if (items is null)
        {
            throw new ArgumentNullException(name);
        }

        var copy = new T[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            copy[i] = items[i] ?? throw new ArgumentException("The list contains a null entry.", name);
        }

        return copy;
    }
}

public sealed class SeatClaims
{
    public SeatClaims(SeatId seat, IReadOnlyList<Mission> missions)
    {
        Seat = seat;
        Missions = Copy(missions);
    }

    public SeatId Seat { get; }

    public IReadOnlyList<Mission> Missions { get; }

    private static Mission[] Copy(IReadOnlyList<Mission>? missions)
    {
        if (missions is null)
        {
            throw new ArgumentNullException(nameof(missions));
        }

        var copy = new Mission[missions.Count];
        for (var i = 0; i < missions.Count; i++)
        {
            copy[i] = missions[i] ?? throw new ArgumentException("The list contains a null entry.", nameof(missions));
        }

        return copy;
    }
}

public sealed class BoardView
{
    public BoardView(IReadOnlyList<OccupiedTile> tiles, IReadOnlyList<OccupiedCell> cells)
    {
        Tiles = Copy(tiles, nameof(tiles));
        Cells = Copy(cells, nameof(cells));
    }

    public IReadOnlyList<OccupiedTile> Tiles { get; }

    public IReadOnlyList<OccupiedCell> Cells { get; }

    private static T[] Copy<T>(IReadOnlyList<T>? items, string name)
    {
        if (items is null)
        {
            throw new ArgumentNullException(name);
        }

        var copy = new T[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            copy[i] = items[i];
        }

        return copy;
    }
}

public readonly record struct OccupiedTile(TileId Id, int TileX, int TileY);

public readonly record struct OccupiedCell(int CellX, int CellY, Cell Value);
