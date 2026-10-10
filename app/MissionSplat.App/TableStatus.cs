namespace MissionSplat.App;

using MissionSplat.Rules;

public enum TableStatusKind
{
    ToConfirm,
    ToAct,
    Won,
    Ended,
    Stopped,
}

// What the table is waiting for. The GUI composes its own words; only a rules or stop message passes through.
public sealed class TableStatus
{
    private TableStatus(TableStatusKind kind, SeatId? seat, string? message)
    {
        Kind = kind;
        Seat = seat;
        Message = message;
    }

    public TableStatusKind Kind { get; }

    public SeatId? Seat { get; }

    public string? Message { get; }

    public static TableStatus ToConfirm(SeatId seat) => new(TableStatusKind.ToConfirm, seat, null);

    public static TableStatus ToAct(SeatId seat) => new(TableStatusKind.ToAct, seat, null);

    public static TableStatus Won(SeatId seat) => new(TableStatusKind.Won, seat, null);

    public static TableStatus Ended() => new(TableStatusKind.Ended, null, null);

    public static TableStatus Stopped(string message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return new TableStatus(TableStatusKind.Stopped, null, message);
    }
}
