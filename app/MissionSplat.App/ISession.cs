namespace MissionSplat.App;

using MissionSplat.Rules;

public interface ISession
{
    // Before Start, CurrentSeat, PowersInPlay, and View throw; the legal-move queries answer empty, and for a seat that is not current.
    SeatId CurrentSeat { get; }

    IReadOnlyList<SymbolId> PowersInPlay { get; }

    SessionResult Start(GameSetup setup);

    SeatView View(SeatId seat);

    SessionResult Submit(SeatId seat, GameAction action);

    IReadOnlyList<LegalPlacement> LegalPlacements(SeatId seat, int quarterTurnsClockwise);

    IReadOnlyList<BoardPosition> LegalTargets(SeatId seat, SymbolId power);

    ActionPreview Preview(SeatId seat, GameAction action);
}
