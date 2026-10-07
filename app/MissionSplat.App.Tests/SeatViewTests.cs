namespace MissionSplat.App.Tests;

using MissionSplat.App;
using MissionSplat.Rules;

public class SeatViewTests
{
    [Test]
    public void View_ShowsThatSeatsUnclaimedMissions_AndEverySeatsClaims()
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));
        var session = ClaimingTable.Open(3).Session;

        var before = session.View(new SeatId("a"));
        Assert.That(Ids(before.UnclaimedMissions), Is.EqualTo(new[] { "a-square", "a-row" }));
        Assert.That(MissionIds.In(before), Does.Contain("a-square"));
        Assert.That(MissionIds.In(before), Does.Contain("a-row"));
        Assert.That(before.Claims.Select(row => row.Seat.Value), Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(before.Claims.Select(row => row.Missions.Count), Is.All.EqualTo(0));
        Assert.That(before.CurrentSeat, Is.EqualTo(new SeatId("a")));
        Assert.That(before.HasEnded, Is.False);
        Assert.That(before.Board.Tiles, Has.Count.EqualTo(1));
        Assert.That(before.Board.Tiles[0].Id.Value, Is.EqualTo("start"));
        Assert.That(before.Board.Tiles[0].TileX, Is.EqualTo(0));
        Assert.That(before.Board.Tiles[0].TileY, Is.EqualTo(0));
        Assert.That(
            before.Board.Cells.Select(cell => (cell.CellX, cell.CellY, cell.Value)),
            Is.EqualTo(new[]
            {
                (0, 0, Cards.Blank),
                (1, 0, Cards.Blank),
                (0, 1, Cards.Blank),
                (1, 1, Cards.Blank),
            }));
        AssertPending(before, "a-tile", Cards.Red);
        AssertAbsent(before, "b-square", "b-row", "c-square", "c-row");

        var placed = session.Place(new SeatId("a"), new Placement(1, 0, 0));
        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);

        var fromB = session.View(new SeatId("b"));
        Assert.That(Ids(fromB.UnclaimedMissions), Is.EqualTo(new[] { "b-square", "b-row" }));
        Assert.That(ClaimIds(fromB, "a"), Is.EqualTo(new[] { "a-square" }));
        Assert.That(ClaimIds(fromB, "b"), Is.Empty);
        Assert.That(ClaimIds(fromB, "c"), Is.Empty);
        AssertAbsent(fromB, "a-row", "a-replacement", "c-square", "c-row", "spare-mission");

        var fromC = session.View(new SeatId("c"));
        Assert.That(ClaimIds(fromC, "a"), Is.EqualTo(new[] { "a-square" }));
        Assert.That(Ids(fromC.UnclaimedMissions), Is.EqualTo(new[] { "c-square", "c-row" }));
        AssertAbsent(fromC, "a-row", "a-replacement", "b-square", "b-row");

        var fromA = session.View(new SeatId("a"));
        Assert.That(Ids(fromA.UnclaimedMissions), Is.EqualTo(new[] { "a-row", "a-replacement" }));
        Assert.That(ClaimIds(fromA, "a"), Is.EqualTo(new[] { "a-square" }));
        AssertAbsent(fromA, "b-square", "b-row", "c-square", "c-row");
    }

    private static void AssertPending(SeatView view, string tileId, Cell cell)
    {
        var pending = view.PendingMatchTile;
        Assert.That(pending, Is.Not.Null);
        Assert.That(pending!.Id.Value, Is.EqualTo(tileId));
        Assert.That(pending.Local(0, 0), Is.EqualTo(cell));
        Assert.That(pending.Local(1, 0), Is.EqualTo(cell));
        Assert.That(pending.Local(0, 1), Is.EqualTo(cell));
        Assert.That(pending.Local(1, 1), Is.EqualTo(cell));
    }

    private static void AssertAbsent(SeatView view, params string[] missionIds)
    {
        var present = MissionIds.In(view);
        foreach (var missionId in missionIds)
        {
            Assert.That(present, Does.Not.Contain(missionId));
        }
    }

    private static string[] Ids(IReadOnlyList<Mission> missions) =>
        missions.Select(mission => mission.Id.Value).ToArray();

    private static string[] ClaimIds(SeatView view, string seat) =>
        Ids(view.Claims.Single(row => row.Seat.Value == seat).Missions);
}
