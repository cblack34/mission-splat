namespace MissionSplat.App.Tests;

using MissionSplat.App;
using MissionSplat.Rules;

public class LocalSessionCommandTests
{
    [Test]
    public void RejectedFirstStart_ReturnsARejection_AndViewThrows()
    {
        var session = new LocalSession();

        var rejected = session.Start(EmptyMatchDeck());

        Assert.That(rejected.IsAccepted, Is.False);
        Assert.That(rejected.Rejection?.RulesRejection?.Reason, Is.EqualTo(RejectionReason.InvalidSetup));
        Assert.That(rejected.Rejection?.Message, Is.EqualTo("The match deck needs a starting tile."));
        Assert.That(rejected.Events, Is.Empty);
        Assert.That(
            () => session.View(new SeatId("a")),
            Throws.InvalidOperationException.With.Message.EqualTo("The session has not started."));
    }

    [Test]
    public void RejectedRestart_LeavesTheAcceptedSession_AndSubmitStillWorks()
    {
        var session = ClaimingTable.Open(3).Session;
        var before = session.View(new SeatId("a"));
        var pending = before.PendingMatchTile?.Id.Value;

        var rejected = session.Start(EmptyMatchDeck());

        Assert.That(rejected.IsAccepted, Is.False);
        Assert.That(rejected.Rejection?.RulesRejection?.Reason, Is.EqualTo(RejectionReason.InvalidSetup));
        Assert.That(rejected.Events, Is.Empty);

        var after = session.View(before.CurrentSeat);
        Assert.That(after.CurrentSeat, Is.EqualTo(before.CurrentSeat));
        Assert.That(after.Board.Tiles, Has.Count.EqualTo(before.Board.Tiles.Count));
        Assert.That(after.PendingMatchTile?.Id.Value, Is.EqualTo(pending));

        var placed = session.Submit(before.CurrentSeat, new Place(1, 0, 0));
        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
        Assert.That(session.View(before.CurrentSeat).Board.Tiles, Has.Count.EqualTo(before.Board.Tiles.Count + 1));
    }

    [Test]
    public void SubmitFromAnotherSeat_IsRejectedByTheRules_AndTheSessionStays()
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));
        var session = ClaimingTable.Open(3).Session;
        var before = Take(session);

        var rejected = session.Submit(new SeatId("b"), new Place(1, 0, 0));

        Assert.That(rejected.IsAccepted, Is.False);
        Assert.That(rejected.Rejection?.Message, Is.EqualTo("It is not that seat's turn."));
        Assert.That(rejected.Rejection?.RulesRejection?.Reason, Is.EqualTo(RejectionReason.NotYourTurn));
        Assert.That(rejected.Events, Is.Empty);
        Assert.That(Take(session), Is.EqualTo(before));

        var accepted = session.Submit(new SeatId("a"), new Place(1, 0, 0));
        Assert.That(accepted.IsAccepted, Is.True, accepted.Rejection?.Message);
        Assert.That(session.View(new SeatId("a")).CurrentSeat, Is.EqualTo(new SeatId("b")));
        Assert.That(session.View(new SeatId("a")).Board.Tiles, Has.Count.EqualTo(2));
    }

    [Test]
    public void RulesRejection_LeavesTheSessionUnchanged()
    {
        var session = ClaimingTable.Open(3).Session;
        var before = Take(session);

        var rejected = session.Submit(new SeatId("a"), new Place(1, 1, 0));

        Assert.That(rejected.IsAccepted, Is.False);
        Assert.That(rejected.Rejection?.RulesRejection?.Reason, Is.EqualTo(RejectionReason.DoesNotShareFullSide));
        Assert.That(rejected.Events, Is.Empty);
        Assert.That(Take(session), Is.EqualTo(before));
    }

    [Test]
    public void Preview_ReportsTheActingClaim_WithoutChangingTheSession()
    {
        var session = ClaimingTable.Open(3).Session;
        var before = Take(session);

        var preview = session.Preview(new SeatId("a"), new Place(1, 0, 0));

        Assert.That(preview.IsAccepted, Is.True, preview.Rejection?.Message);
        Assert.That(preview.ClaimedMissions.Select(mission => mission.Value), Is.EqualTo(new[] { "a-square" }));
        Assert.That(preview.ClaimedMissions.Select(mission => mission.Value), Does.Not.Contain("b-square"));
        Assert.That(preview.ClaimedMissions.Select(mission => mission.Value), Does.Not.Contain("a-row"));
        Assert.That(Take(session), Is.EqualTo(before));

        var placed = session.Submit(new SeatId("a"), new Place(1, 0, 0));
        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
        Assert.That(session.View(new SeatId("b")).CurrentSeat, Is.EqualTo(new SeatId("b")));
        Assert.That(session.View(new SeatId("b")).Board.Tiles, Has.Count.EqualTo(2));
        Assert.That(
            session.View(new SeatId("b")).Claims.Single(row => row.Seat.Value == "a").Missions.Select(mission => mission.Id.Value),
            Is.EqualTo(new[] { "a-square" }));
    }

    [Test]
    public void AStackPlacement_ShowsOnlyTheTopTile_AndItsFourCells_OnTheBoard()
    {
        var session = new LocalSession();
        var lid = Cards.Tile("lid", Cell.Symbol(OrdinaryCatalog.Stack), Cards.Red, Cards.Red, Cards.Red);
        var started = session.Start(RepresentativeDeck.Setup(
            ["a", "b"],
            "a",
            [Cards.Row("a1"), Cards.Row("a2"), Cards.Row("b1"), Cards.Row("b2"), Cards.Row("spare")],
            [Cards.BlankTile("start"), lid]));
        Assert.That(started.IsAccepted, Is.True, started.Rejection?.Message);

        var stacked = session.Submit(new SeatId("a"), new Place(0, 0, 0));

        Assert.That(stacked.IsAccepted, Is.True, stacked.Rejection?.Message);
        var board = session.View(new SeatId("b")).Board;
        Assert.That(board.Tiles.Select(tile => (tile.Id.Value, tile.TileX, tile.TileY)), Is.EqualTo(new[] { ("lid", 0, 0) }));
        Assert.That(board.Cells, Has.Count.EqualTo(4));
        Assert.That(board.Cells.Select(cell => cell.Value), Does.Contain(Cards.Red));
    }

    [Test]
    public void PreviewFromAnotherSeat_IsRejected_AndNamesNoMission()
    {
        var session = ClaimingTable.Open(3).Session;
        var before = Take(session);

        var preview = session.Preview(new SeatId("c"), new Place(1, 0, 0));

        Assert.That(preview.IsAccepted, Is.False);
        Assert.That(preview.Rejection?.RulesRejection?.Reason, Is.EqualTo(RejectionReason.NotYourTurn));
        Assert.That(preview.ClaimedMissions, Is.Empty);
        Assert.That(Take(session), Is.EqualTo(before));
    }

    [Test]
    public void BeforeStart_TheCurrentSeatThrows_AndSubmitPreviewAndTheLegalQueriesAnswerWithoutOne()
    {
        var session = new LocalSession();
        var seat = new SeatId("a");

        Assert.That(() => session.CurrentSeat, Throws.InvalidOperationException);
        Assert.That(() => session.PowersInPlay, Throws.InvalidOperationException);
        var submitted = session.Submit(seat, new Place(0, 0, 0));
        Assert.That(submitted.IsAccepted, Is.False);
        Assert.That(submitted.Rejection?.Message, Is.EqualTo("The session has not started."));
        Assert.That(session.Preview(seat, new Place(0, 0, 0)).IsAccepted, Is.False);
        Assert.That(session.LegalPlacements(seat, 0), Is.Empty);
        Assert.That(session.LegalTargets(seat, OrdinaryCatalog.Rotate), Is.Empty);
    }

    [Test]
    public void TheSession_NamesTheCurrentSeat_AndThePowersTheSetupLists()
    {
        var session = ClaimingTable.Open(3).Session;

        Assert.That(session.CurrentSeat, Is.EqualTo(new SeatId("a")));
        Assert.That(
            session.PowersInPlay,
            Is.EqualTo(new[] { OrdinaryCatalog.Rotate, OrdinaryCatalog.Stack, OrdinaryCatalog.Bounce }));
        session.Submit(new SeatId("a"), new Place(1, 0, 0));
        Assert.That(session.CurrentSeat, Is.EqualTo(new SeatId("b")));
    }

    [Test]
    public void LegalPlacements_ListEveryOrthogonalNeighbor_ForTheCurrentSeatOnly()
    {
        var session = ClaimingTable.Open(3).Session;

        Assert.That(
            session.LegalPlacements(new SeatId("a"), 0),
            Is.EqualTo(new[]
            {
                new LegalPlacement(-1, 0, PlacementKind.Beside),
                new LegalPlacement(0, -1, PlacementKind.Beside),
                new LegalPlacement(0, 1, PlacementKind.Beside),
                new LegalPlacement(1, 0, PlacementKind.Beside),
            }));
        Assert.That(session.LegalPlacements(new SeatId("b"), 0), Is.Empty);
        Assert.That(session.LegalTargets(new SeatId("b"), OrdinaryCatalog.Rotate), Is.Empty);
        Assert.That(session.LegalPlacements(new SeatId("a"), 4), Is.Empty);
    }

    [Test]
    public void ARotateSubmitted_SpendsItsCharge_KeepsTheSeat_AndTheTargetsFollow()
    {
        var session = new LocalSession();
        var seat = new SeatId("a");
        var started = session.Start(RepresentativeDeck.Setup(
            ["a", "b"],
            "a",
            [Cards.Row("a1"), Cards.Row("a2"), Cards.Row("b1"), Cards.Row("b2"), Cards.Row("spare")],
            [Cards.BlankTile("start"), Cards.Tile("drawn", Cell.Symbol(OrdinaryCatalog.Rotate), Cards.Red, Cards.Red, Cards.Red)]));
        Assert.That(started.IsAccepted, Is.True, started.Rejection?.Message);
        Assert.That(
            session.View(seat).RemainingUses,
            Is.EqualTo(new[] { new PowerCharge(OrdinaryCatalog.Rotate, 1), new PowerCharge(OrdinaryCatalog.Bounce, 0) }));
        Assert.That(session.LegalTargets(seat, OrdinaryCatalog.Rotate), Is.EqualTo(new[] { new BoardPosition(0, 0) }));
        var preview = session.Preview(seat, new UseRotate(0, 0, 1));
        Assert.That(preview.IsAccepted, Is.True, preview.Rejection?.Message);
        Assert.That(preview.ClaimedMissions, Is.Empty);
        Assert.That(session.View(seat).RemainingUses[0].Remaining, Is.EqualTo(1));

        var rotated = session.Submit(seat, new UseRotate(0, 0, 1));

        Assert.That(rotated.IsAccepted, Is.True, rotated.Rejection?.Message);
        Assert.That(rotated.Events, Is.EqualTo(new GameEvent[] { new TileRotated(seat, 0, 0, 1) }));
        Assert.That(session.CurrentSeat, Is.EqualTo(seat));
        Assert.That(session.View(seat).RemainingUses[0], Is.EqualTo(new PowerCharge(OrdinaryCatalog.Rotate, 0)));
        Assert.That(session.LegalTargets(seat, OrdinaryCatalog.Rotate), Is.Empty);
        Assert.That(session.Submit(seat, new UseRotate(0, 0, 1)).Rejection?.RulesRejection?.Reason, Is.EqualTo(RejectionReason.NoUseRemaining));
    }

    private static GameSetup EmptyMatchDeck()
    {
        return RepresentativeDeck.Setup(
            ["a", "b"],
            "a",
            [
                Cards.Mission("a-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Row("a-row"),
                Cards.Mission("b-square", MissionPattern.Square, OrdinaryCatalog.Blue),
                Cards.Row("b-row"),
            ],
            []);
    }

    private static SessionPicture Take(LocalSession session)
    {
        var seats = new[] { "a", "b", "c" };
        var first = session.View(new SeatId(seats[0]));
        var hands = string.Join(
            ";",
            seats.Select(seat =>
            {
                var view = session.View(new SeatId(seat));
                return seat + "=" + string.Join(",", view.UnclaimedMissions.Select(mission => mission.Id.Value));
            }));
        var claims = string.Join(
            ";",
            first.Claims.Select(row => row.Seat.Value + "=" + string.Join(",", row.Missions.Select(mission => mission.Id.Value))));
        var cells = string.Join(
            ";",
            first.Board.Cells.Select(cell => cell.CellX + "," + cell.CellY + ":" + cell.Value));

        return new SessionPicture(
            first.CurrentSeat.Value,
            first.HasEnded,
            first.Board.Tiles.Count,
            first.PendingMatchTile?.Id.Value,
            hands,
            claims,
            cells);
    }

    private sealed record SessionPicture(
        string CurrentSeat,
        bool HasEnded,
        int TileCount,
        string? PendingTile,
        string Hands,
        string Claims,
        string Cells);
}
