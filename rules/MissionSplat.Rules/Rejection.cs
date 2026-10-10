namespace MissionSplat.Rules;

public enum RejectionReason
{
    InvalidSetup,
    GameOver,
    NotYourTurn,
    InvalidQuarterTurns,
    NotAtOrigin,
    DoesNotShareFullSide,
    CellOccupied,
    NoUseRemaining,
    InvalidRotateQuarterTurns,
    NoTileToRotate,
    TileSurrounded,
    NoTileToBounce,
}

public sealed record Rejection(RejectionReason Reason, string Message);
