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
}

public sealed record Rejection(RejectionReason Reason, string Message);
