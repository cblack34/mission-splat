namespace MissionSplat.Rules;

public abstract record GameEvent;

public sealed record TilePlaced(
    SeatId Seat,
    TileId Tile,
    int TileX,
    int TileY,
    int QuarterTurnsClockwise) : GameEvent;

public sealed record MissionClaimed(SeatId Seat, MissionId Mission, MissionId Replacement) : GameEvent;

public sealed record GameWon(SeatId Seat, int ClaimCount) : GameEvent;
