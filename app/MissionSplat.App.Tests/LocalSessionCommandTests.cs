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
    public void RejectedRestart_LeavesTheAcceptedSession_AndPlaceStillWorks()
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

        var placed = session.Place(before.CurrentSeat, new Placement(1, 0, 0));
        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
        Assert.That(session.View(before.CurrentSeat).Board.Tiles, Has.Count.EqualTo(before.Board.Tiles.Count + 1));
    }

    [Test]
    public void PlaceFromAnotherSeat_IsRejected_AndTheSessionStays()
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));
        var session = ClaimingTable.Open(3).Session;
        var before = Take(session);

        var rejected = session.Place(new SeatId("b"), new Placement(1, 0, 0));

        Assert.That(rejected.IsAccepted, Is.False);
        Assert.That(rejected.Rejection?.Message, Is.EqualTo("It is not that seat's turn."));
        Assert.That(rejected.Rejection?.RulesRejection, Is.Null);
        Assert.That(rejected.Events, Is.Empty);
        Assert.That(Take(session), Is.EqualTo(before));

        var accepted = session.Place(new SeatId("a"), new Placement(1, 0, 0));
        Assert.That(accepted.IsAccepted, Is.True, accepted.Rejection?.Message);
        Assert.That(session.View(new SeatId("a")).CurrentSeat, Is.EqualTo(new SeatId("b")));
        Assert.That(session.View(new SeatId("a")).Board.Tiles, Has.Count.EqualTo(2));
    }

    [Test]
    public void RulesRejection_LeavesTheSessionUnchanged()
    {
        var session = ClaimingTable.Open(3).Session;
        var before = Take(session);

        var rejected = session.Place(new SeatId("a"), new Placement(1, 1, 0));

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

        var preview = session.Preview(new SeatId("a"), new Placement(1, 0, 0));

        Assert.That(preview.IsAccepted, Is.True, preview.Rejection?.Message);
        Assert.That(preview.ClaimedMissions.Select(mission => mission.Value), Is.EqualTo(new[] { "a-square" }));
        Assert.That(preview.ClaimedMissions.Select(mission => mission.Value), Does.Not.Contain("b-square"));
        Assert.That(preview.ClaimedMissions.Select(mission => mission.Value), Does.Not.Contain("a-row"));
        Assert.That(Take(session), Is.EqualTo(before));

        var placed = session.Place(new SeatId("a"), new Placement(1, 0, 0));
        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
        Assert.That(session.View(new SeatId("b")).CurrentSeat, Is.EqualTo(new SeatId("b")));
        Assert.That(session.View(new SeatId("b")).Board.Tiles, Has.Count.EqualTo(2));
        Assert.That(
            session.View(new SeatId("b")).Claims.Single(row => row.Seat.Value == "a").Missions.Select(mission => mission.Id.Value),
            Is.EqualTo(new[] { "a-square" }));
    }

    [Test]
    public void PreviewFromAnotherSeat_IsRejected_AndNamesNoMission()
    {
        var session = ClaimingTable.Open(3).Session;
        var before = Take(session);

        var preview = session.Preview(new SeatId("c"), new Placement(1, 0, 0));

        Assert.That(preview.IsAccepted, Is.False);
        Assert.That(preview.Rejection?.RulesRejection, Is.Null);
        Assert.That(preview.ClaimedMissions, Is.Empty);
        Assert.That(Take(session), Is.EqualTo(before));
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
