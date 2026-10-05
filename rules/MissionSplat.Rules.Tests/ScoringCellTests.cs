namespace MissionSplat.Rules.Tests;

public class ScoringCellTests
{
    [TestCase("blank")]
    [TestCase("rotate")]
    [TestCase("stack")]
    [TestCase("bounce")]
    public void NonScoringSymbol_BlocksAnOtherwiseCompleteRow(string symbol)
    {
        var blocked = PlaceBlocker(Cards.Symbol(symbol), OrdinaryCatalog.Colors, OrdinaryCatalog.NonScoringSymbols);

        Assert.That(blocked.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(See.Ids(blocked.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row", "a2" }));
        Assert.That(blocked.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(blocked.TileCount, Is.EqualTo(2));
    }

    [Test]
    public void CallerSuppliedSymbol_DoesNotCompleteAPattern()
    {
        var spark = new SymbolId("spark");
        var blocked = PlaceBlocker(
            Cell.Symbol(spark),
            [OrdinaryCatalog.Red, OrdinaryCatalog.Purple],
            [OrdinaryCatalog.Blank, spark]);

        Assert.That(blocked.CellAt(3, 0), Is.EqualTo(Cell.Symbol(spark)));
        Assert.That(blocked.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(blocked.CellAt(3, 0)?.TryGetColor(out _), Is.False);
    }

    [Test]
    public void CallerSuppliedColor_MatchesWithoutTheOrdinaryFour()
    {
        var amber = new ColorId("amber");
        var amberCell = Cell.Color(amber);
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("amber-row", MissionPattern.Row, amber),
                Cards.Mission("amber-square", MissionPattern.Square, amber),
                Cards.Mission("b1", MissionPattern.L, amber),
                Cards.Mission("b2", MissionPattern.L, amber),
                Cards.Mission("repl", MissionPattern.L, amber),
            ],
            [
                Cards.Tile("start", amberCell, amberCell, Cards.Blank, Cards.Blank),
                Cards.Tile("finish", amberCell, amberCell, Cards.Blank, Cards.Blank),
            ],
            colors: [amber],
            symbols: [OrdinaryCatalog.Blank]);

        var next = See.Game(RepresentativeDeck.Play(game, 1, 0));

        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "amber-row" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Does.Contain("amber-square"));
    }

    [Test]
    public void SquareOnlyGame_DoesNotClaimARow_AndStillClaimsASquare()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Mission("red-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("repl"),
                Cards.Purple("spare"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Solid("finish", Cards.Red),
            ],
            patterns: [MissionPattern.Square]);

        var next = See.Game(RepresentativeDeck.Play(game, 1, 0));

        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-square" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row", "repl" }));
    }

    [Test]
    public void Wildcard_CountsAsEachMissionColor_AndStaysWild()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Mission("blue-row", MissionPattern.Row, OrdinaryCatalog.Blue),
                Cards.Purple("b2"),
                Cards.Purple("repl-a"),
                Cards.Purple("repl-b"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Wild, Cards.Blank, Cards.Blue),
                Cards.Tile("reds", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("blues", Cards.Blank, Cards.Blue, Cards.Blank, Cards.Blue),
            ]);

        var redClaim = RepresentativeDeck.Play(game, 1, 0);
        var afterRed = See.Game(redClaim);
        Assert.That(See.Ids(afterRed.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row" }));
        Assert.That(afterRed.Claims(Cards.Seat("b")), Is.Empty);
        AssertWild(afterRed);

        var blueClaim = RepresentativeDeck.Play(afterRed, 0, 1);
        var afterBlue = See.Game(blueClaim);
        Assert.That(See.Ids(afterBlue.Claims(Cards.Seat("b"))), Is.EqualTo(new[] { "blue-row" }));
        Assert.That(See.Ids(afterBlue.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row" }));
        Assert.That(afterBlue.CellAt(1, 2), Is.EqualTo(Cards.Blue));
        Assert.That(afterBlue.CellAt(1, 3), Is.EqualTo(Cards.Blue));
        AssertWild(afterBlue);
    }

    private static void AssertWild(Game game)
    {
        var wild = game.CellAt(1, 0);
        Assert.That(wild?.IsWild, Is.True);
        Assert.That(wild?.TryGetColor(out _), Is.False);
        Assert.That(wild?.CountsAs(OrdinaryCatalog.Red), Is.True);
        Assert.That(wild?.CountsAs(OrdinaryCatalog.Blue), Is.True);
    }

    private static Game PlaceBlocker(
        Cell blocker,
        IReadOnlyList<ColorId> colors,
        IReadOnlyList<SymbolId> symbols)
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("spare"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("blocked", Cards.Red, blocker, Cards.Blank, Cards.Blank),
            ],
            colors: colors,
            symbols: symbols);
        return See.Game(RepresentativeDeck.Play(game, 1, 0));
    }
}
