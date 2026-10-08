namespace MissionSplat.Rules;

public enum RejectionReason
{
    InvalidSetup,
    DoesNotShareFullSide,
    CellOccupied,
    GameOver,
    InvalidQuarterTurns,
    NoStackCell,
    NoTileToCover,
    NoRotateUse,
    NoRotateCell,
    TooManyRotateUses,
    InvalidRotateQuarterTurns,
    NoTileToRotate,
    TileSurrounded,
}

public sealed record Rejection(RejectionReason Reason, string Message);
