namespace MissionSplat.Rules;

public abstract record GameEvent;

public sealed record TileRotated(SeatId Seat, int TileX, int TileY, int QuarterTurnsClockwise) : GameEvent;

public sealed record TileBounced(SeatId Seat, int TileX, int TileY, TileId Removed, TileId? Revealed) : GameEvent;

public sealed record TilePlaced(
    SeatId Seat,
    TileId Tile,
    int TileX,
    int TileY,
    int QuarterTurnsClockwise,
    TileId? Covered) : GameEvent;

public sealed record MissionClaimed(SeatId Seat, MissionId Mission) : GameEvent;

public sealed record GameWon(SeatId Seat, int ClaimCount) : GameEvent;
