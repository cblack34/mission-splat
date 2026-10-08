namespace MissionSplat.Rules;

// Zero quarter-turns orients a placement. It is not a rotate use, so this value stays out of InvalidQuarterTurns.
public readonly record struct RotateUse(int TileX, int TileY, int QuarterTurnsClockwise);
