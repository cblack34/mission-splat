namespace MissionSplat.App;

using MissionSplat.Rules;

// The action source for an automated seat. A human seat is a GUI submitting through the table.
public interface IPlayer
{
    SeatId Seat { get; }

    GameAction ChooseAction(SeatView view);
}
