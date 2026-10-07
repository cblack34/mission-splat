namespace MissionSplat.App;

using MissionSplat.Rules;

public interface IPlayer
{
    SeatId Seat { get; }

    Placement ChoosePlacement(SeatView view);
}
