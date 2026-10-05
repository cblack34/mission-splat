namespace MissionSplat.Rules.Tests;

public class ResolutionTests
{
    [Test]
    public void OneTile_ClaimsBothMissionsTheActingSeatHolds_AndPassesOnce()
    {
        var placed = RepresentativeDeck.Play(RowAndSquareGame(), 1, 0);
        var next = See.Game(placed);

        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row", "red-square" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "repl-1", "repl-2" }));
        Assert.That(next.Hand(Cards.Seat("a")), Has.Count.EqualTo(2));
        Assert.That(next.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(placed.Events.OfType<TilePlaced>().Count(), Is.EqualTo(1));
        Assert.That(
            placed.Events.OfType<MissionClaimed>().Select(claim => claim.Mission.Value).ToArray(),
            Is.EqualTo(new[] { "red-row", "red-square" }));
        Assert.That(placed.Events.OfType<GameWon>(), Is.Empty);
        Assert.That(next.HasEnded, Is.False);
    }

    [Test]
    public void ReplacementDrawnDuringResolution_IsNotClaimedEvenWhenTheBoardShowsIt()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Mission("red-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Purple("spare"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Solid("finish", Cards.Red),
            ]);

        var placed = RepresentativeDeck.Play(game, 1, 0);
        var next = See.Game(placed);

        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "a2", "red-square" }));
        Assert.That(placed.Events.OfType<MissionClaimed>().Count(), Is.EqualTo(1));
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
    }

    [TestCase(2)]
    [TestCase(3)]
    public void StolenPattern_IsNotClaimed_EvenAfterALaterTileMissesIt(int seatCount)
    {
        var names = new[] { "a", "b", "c" }.Take(seatCount).ToArray();
        var missions = new List<Mission>
        {
            Cards.Purple("a1"),
            Cards.Purple("a2"),
            Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
            Cards.Purple("b2"),
        };
        for (var i = 2; i < seatCount; i++)
        {
            missions.Add(Cards.Purple(names[i] + "1"));
            missions.Add(Cards.Purple(names[i] + "2"));
        }

        missions.Add(Cards.Purple("spare"));
        var game = RepresentativeDeck.Start(
            names,
            "a",
            missions.ToArray(),
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("stolen", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.BlankTile("miss"),
            ]);

        Assert.That(See.Ids(game.Hand(Cards.Seat("b"))), Does.Contain("red-row"));

        var stolen = See.Game(RepresentativeDeck.Play(game, 1, 0));
        Assert.That(stolen.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(stolen.CellAt(3, 0), Is.EqualTo(Cards.Red));
        AssertNoClaims(stolen, names);

        var missed = See.Game(RepresentativeDeck.Play(stolen, 2, 0));
        Assert.That(missed.CellAt(4, 0), Is.EqualTo(Cards.Blank));
        Assert.That(missed.CellAt(5, 0), Is.EqualTo(Cards.Blank));
        AssertNoClaims(missed, names);
        Assert.That(See.Ids(missed.Hand(Cards.Seat("b"))), Does.Contain("red-row"));
    }

    private static Game RowAndSquareGame() =>
        RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Mission("red-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("repl-1"),
                Cards.Purple("repl-2"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Solid("finish", Cards.Red),
            ]);

    private static void AssertNoClaims(Game game, IReadOnlyList<string> names)
    {
        foreach (var name in names)
        {
            Assert.That(game.Claims(Cards.Seat(name)), Is.Empty);
        }
    }
}
