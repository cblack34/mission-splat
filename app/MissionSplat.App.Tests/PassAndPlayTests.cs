namespace MissionSplat.App.Tests;

using MissionSplat.App;
using MissionSplat.Rules;

public class PassAndPlayTests
{
    [TestCase(3)]
    [TestCase(4)]
    public void ScriptedPlayers_CompleteARound_AndEachSeatClaims(int seatCount)
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));
        var table = ClaimingTable.Open(seatCount);
        var session = table.Session;

        for (var i = 0; i < table.Players.Length; i++)
        {
            var player = table.Players[i];
            var view = session.View(player.Seat);
            Assert.That(view.CurrentSeat, Is.EqualTo(player.Seat));
            Assert.That(view.HasEnded, Is.False);

            var result = session.Submit(player.Seat, player.ChooseAction(view));
            Assert.That(result.IsAccepted, Is.True, result.Rejection?.Message);
            Assert.That(result.Events.OfType<MissionClaimed>().Select(claim => claim.Mission.Value), Is.EqualTo(new[] { player.Seat.Value + "-square" }));

            var next = table.Players[(i + 1) % table.Players.Length];
            Assert.That(session.View(player.Seat).CurrentSeat, Is.EqualTo(next.Seat));
        }

        var published = session.View(table.Players[0].Seat);
        Assert.That(published.CurrentSeat, Is.EqualTo(table.Players[0].Seat));
        Assert.That(published.HasEnded, Is.False);
        Assert.That(published.Board.Tiles, Has.Count.EqualTo(seatCount + 1));
        Assert.That(
            published.Board.Cells.Single(cell => cell.CellX == 2 && cell.CellY == 0).Value,
            Is.EqualTo(Cards.Red));

        foreach (var player in table.Players)
        {
            var row = published.Claims.Single(claim => claim.Seat.Equals(player.Seat));
            Assert.That(row.Missions.Select(mission => mission.Id.Value), Is.EqualTo(new[] { player.Seat.Value + "-square" }));
        }
    }
}
