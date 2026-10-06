namespace MissionSplat.Rules.Tests;

public class RowClaimTests
{
    [Test]
    public void HorizontalRow_ClaimsForTheFinisher_DrawsOneReplacement_AndPassesTheTurn()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("repl"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("finish", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
            ]);

        var placed = RepresentativeDeck.Play(game, 1, 0);
        var next = See.Game(placed);

        Assert.That(next.HasTileAt(0, 0), Is.True);
        Assert.That(next.HasTileAt(1, 0), Is.True);
        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(3, 0), Is.EqualTo(Cards.Red));
        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "a2", "repl" }));
        Assert.That(next.Hand(Cards.Seat("a")), Has.Count.EqualTo(2));
        Assert.That(next.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(placed.Events, Has.Count.EqualTo(2));
        Assert.That(placed.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(((MissionClaimed)placed.Events[1]).Mission.Value, Is.EqualTo("red-row"));
        Assert.That(placed.Events.OfType<GameWon>(), Is.Empty);
    }

    [Test]
    public void VerticalRow_CrossesATileBoundary_AndOnlyTheFinisherClaims()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("repl"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Blank, Cards.Red, Cards.Blank),
                Cards.Tile("finish", Cards.Red, Cards.Blank, Cards.Red, Cards.Blank),
            ]);

        var next = See.Game(RepresentativeDeck.Play(game, 0, 1));

        Assert.That(next.HasTileAt(0, 0), Is.True);
        Assert.That(next.HasTileAt(0, 1), Is.True);
        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(0, 3), Is.EqualTo(Cards.Red));
        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row" }));
        Assert.That(next.Claims(Cards.Seat("b")), Is.Empty);
    }

    [Test]
    public void MainDiagonal_CrossesTiles_AndOnlyTheFinisherClaims()
    {
        // Cells (1,0), (2,1), (3,2), (4,3). Seat b holds the row, writes (3,2), then later only (4,3).
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Purple("a1"),
                Cards.Purple("a2"),
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Purple("b2"),
                Cards.Purple("repl"),
            ],
            [
                Cards.Tile("start", Cards.Blank, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("a-bridge", Cards.Blank, Cards.Blank, Cards.Red, Cards.Blank),
                Cards.Tile("b-third", Cards.Blank, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.BlankTile("a-side"),
                Cards.Tile("finish", Cards.Blank, Cards.Blank, Cards.Red, Cards.Blank),
            ]);

        var afterA = See.Game(RepresentativeDeck.Play(game, 1, 0));
        Assert.That(afterA.CellAt(2, 1), Is.EqualTo(Cards.Red));
        Assert.That(afterA.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(afterA.Claims(Cards.Seat("b")), Is.Empty);

        var third = RepresentativeDeck.Play(afterA, 1, 1);
        var afterThird = See.Game(third);
        Assert.That(afterThird.CellAt(1, 0), Is.EqualTo(Cards.Red));
        Assert.That(afterThird.CellAt(2, 1), Is.EqualTo(Cards.Red));
        Assert.That(afterThird.CellAt(3, 2), Is.EqualTo(Cards.Red));
        Assert.That(afterThird.CellAt(4, 3), Is.Null);
        Assert.That(afterThird.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(See.Ids(afterThird.Hand(Cards.Seat("b"))), Is.EqualTo(new[] { "red-row", "b2" }));
        Assert.That(third.Events.OfType<MissionClaimed>(), Is.Empty);

        var afterSide = See.Game(RepresentativeDeck.Play(afterThird, 2, 0));
        Assert.That(afterSide.CellAt(4, 3), Is.Null);
        Assert.That(afterSide.Claims(Cards.Seat("a")), Is.Empty);

        var finished = See.Game(RepresentativeDeck.Play(afterSide, 2, 1));
        Assert.That(finished.CellAt(1, 0), Is.EqualTo(Cards.Red));
        Assert.That(finished.CellAt(2, 1), Is.EqualTo(Cards.Red));
        Assert.That(finished.CellAt(3, 2), Is.EqualTo(Cards.Red));
        Assert.That(finished.CellAt(4, 3), Is.EqualTo(Cards.Red));
        Assert.That(See.Ids(finished.Claims(Cards.Seat("b"))), Is.EqualTo(new[] { "red-row" }));
        Assert.That(See.Ids(finished.Hand(Cards.Seat("b"))), Is.EqualTo(new[] { "b2", "repl" }));
        Assert.That(finished.Claims(Cards.Seat("a")), Is.Empty);
    }

    [Test]
    public void AntiDiagonal_CrossesTiles_AndOnlyTheFinisherClaims()
    {
        // Cells (0,4), (1,3), (2,2), (3,1), step (+1,-1). Seat a holds the row and writes (2,2) before (3,1).
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            RowMissions(),
            [
                Cards.BlankTile("start"),
                Cards.Tile("a-low", Cards.Blank, Cards.Blank, Cards.Blank, Cards.Red),
                Cards.Tile("b-high", Cards.Red, Cards.Blank, Cards.Blank, Cards.Blank),
                Cards.Tile("a-third", Cards.Red, Cards.Blank, Cards.Blank, Cards.Blank),
                Cards.BlankTile("b-side"),
                Cards.Tile("finish", Cards.Blank, Cards.Blank, Cards.Blank, Cards.Red),
            ]);

        var afterLow = See.Game(RepresentativeDeck.Play(game, 0, 1));
        Assert.That(afterLow.CellAt(1, 3), Is.EqualTo(Cards.Red));
        Assert.That(afterLow.Claims(Cards.Seat("a")), Is.Empty);

        var afterHigh = See.Game(RepresentativeDeck.Play(afterLow, 0, 2));
        Assert.That(afterHigh.CellAt(0, 4), Is.EqualTo(Cards.Red));
        Assert.That(afterHigh.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(afterHigh.Claims(Cards.Seat("b")), Is.Empty);

        var third = RepresentativeDeck.Play(afterHigh, 1, 1);
        var afterThird = See.Game(third);
        Assert.That(afterThird.CellAt(0, 4), Is.EqualTo(Cards.Red));
        Assert.That(afterThird.CellAt(1, 3), Is.EqualTo(Cards.Red));
        Assert.That(afterThird.CellAt(2, 2), Is.EqualTo(Cards.Red));
        Assert.That(afterThird.CellAt(3, 1), Is.Null);
        Assert.That(afterThird.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(See.Ids(afterThird.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row", "a2" }));
        Assert.That(third.Events.OfType<MissionClaimed>(), Is.Empty);

        var afterSide = See.Game(RepresentativeDeck.Play(afterThird, 1, 2));
        Assert.That(afterSide.CellAt(3, 1), Is.Null);
        Assert.That(afterSide.Claims(Cards.Seat("b")), Is.Empty);

        var finished = See.Game(RepresentativeDeck.Play(afterSide, 1, 0));
        Assert.That(finished.CellAt(0, 4), Is.EqualTo(Cards.Red));
        Assert.That(finished.CellAt(1, 3), Is.EqualTo(Cards.Red));
        Assert.That(finished.CellAt(2, 2), Is.EqualTo(Cards.Red));
        Assert.That(finished.CellAt(3, 1), Is.EqualTo(Cards.Red));
        Assert.That(See.Ids(finished.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row" }));
        Assert.That(See.Ids(finished.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "a2", "repl" }));
        Assert.That(finished.Claims(Cards.Seat("b")), Is.Empty);
    }

    private static Mission[] RowMissions() =>
    [
        Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
        Cards.Purple("a2"),
        Cards.Purple("b1"),
        Cards.Purple("b2"),
        Cards.Purple("repl"),
    ];
}
