namespace MissionSplat.Rules;

public enum RejectionReason
{
    InvalidSetup,
    DoesNotShareFullSide,
    CellOccupied,
    GameOver,
    InvalidQuarterTurns,
}

public sealed record Rejection(RejectionReason Reason, string Message);
