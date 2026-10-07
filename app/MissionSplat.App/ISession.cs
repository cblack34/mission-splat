namespace MissionSplat.App;

using MissionSplat.Rules;

public interface ISession
{
    SessionResult Start(GameSetup setup);

    SeatView View(SeatId seat);

    SessionResult Place(SeatId seat, Placement placement);

    PlacementPreview Preview(SeatId seat, Placement placement);
}
