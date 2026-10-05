namespace MissionSplat.Rules.Tests;

public class SquareClaimTests
{
    [Test]
    public void FourTileCorner_ClaimsOnlyForTheSeatWhoPlacesTheLastCorner()
    {
        // The meeting cells are (1,1), (2,1), (1,2), and (2,2), one from each tile.
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("repl"),
            ],
            [
                Cards.Tile("start", Cards.Blank, Cards.Blank, Cards.Blank, Cards.Red),
                Cards.Tile("a-side", Cards.Blank, Cards.Blank, Cards.Red, Cards.Blank),
                Cards.Tile("b-side", Cards.Blank, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("finish", Cards.Red, Cards.Blank, Cards.Blank, Cards.Blank),
            ]);

        var afterA = See.Game(RepresentativeDeck.Play(game, 1, 0));
        Assert.That(afterA.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(afterA.Claims(Cards.Seat("b")), Is.Empty);

        var afterB = See.Game(RepresentativeDeck.Play(afterA, 0, 1));
        Assert.That(afterB.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(afterB.Claims(Cards.Seat("b")), Is.Empty);

        var placed = RepresentativeDeck.Play(afterB, 1, 1);
        var finished = See.Game(placed);
        var corners = new[] { (1, 1), (2, 1), (1, 2), (2, 2) };
        Assert.That(corners.Select(cell => (cell.Item1 / 2, cell.Item2 / 2)).Distinct().Count(), Is.EqualTo(4));
        foreach (var (x, y) in corners)
        {
            Assert.That(finished.CellAt(x, y), Is.EqualTo(Cards.Red));
            Assert.That(finished.HasTileAt(x / 2, y / 2), Is.True);
        }

        Assert.That(See.Ids(finished.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-square" }));
        Assert.That(finished.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(finished.Hand(Cards.Seat("a")), Has.Count.EqualTo(2));
        Assert.That(placed.Events.OfType<MissionClaimed>().Single().Mission.Value, Is.EqualTo("red-square"));
    }
}
