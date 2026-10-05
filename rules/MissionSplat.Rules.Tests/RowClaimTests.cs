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
        Assert.That(((MissionClaimed)placed.Events[1]).Replacement.Value, Is.EqualTo("repl"));
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
        // Cells (1,0), (2,1), (3,2), (4,3). The last tile belongs to seat a.
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            RowMissions(),
            [
                Cards.Tile("start", Cards.Blank, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("a-bridge", Cards.Blank, Cards.Blank, Cards.Red, Cards.Blank),
                Cards.Tile("b-bridge", Cards.Blank, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("finish", Cards.Blank, Cards.Blank, Cards.Red, Cards.Blank),
            ]);

        var afterA = See.Game(RepresentativeDeck.Play(game, 1, 0));
        Assert.That(afterA.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(afterA.Claims(Cards.Seat("b")), Is.Empty);

        var afterB = See.Game(RepresentativeDeck.Play(afterA, 1, 1));
        Assert.That(afterB.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(afterB.Claims(Cards.Seat("b")), Is.Empty);

        var finished = See.Game(RepresentativeDeck.Play(afterB, 2, 1));
        Assert.That(finished.CellAt(1, 0), Is.EqualTo(Cards.Red));
        Assert.That(finished.CellAt(2, 1), Is.EqualTo(Cards.Red));
        Assert.That(finished.CellAt(3, 2), Is.EqualTo(Cards.Red));
        Assert.That(finished.CellAt(4, 3), Is.EqualTo(Cards.Red));
        Assert.That(See.Ids(finished.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row" }));
        Assert.That(finished.Claims(Cards.Seat("b")), Is.Empty);
    }

    [Test]
    public void AntiDiagonal_CrossesTiles_AndOnlyTheFinisherClaims()
    {
        // Cells (0,5), (1,4), (2,3), (3,2). Seat a places the last two.
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            RowMissions(),
            [
                Cards.BlankTile("start"),
                Cards.BlankTile("a-bridge"),
                Cards.Tile("b-pair", Cards.Blank, Cards.Red, Cards.Red, Cards.Blank),
                Cards.Tile("finish", Cards.Blank, Cards.Red, Cards.Red, Cards.Blank),
            ]);

        var afterBridge = See.Game(RepresentativeDeck.Play(game, 0, 1));
        Assert.That(afterBridge.Claims(Cards.Seat("a")), Is.Empty);

        var afterB = See.Game(RepresentativeDeck.Play(afterBridge, 0, 2));
        Assert.That(afterB.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(afterB.Claims(Cards.Seat("a")), Is.Empty);

        var finished = See.Game(RepresentativeDeck.Play(afterB, 1, 1));
        Assert.That(finished.CellAt(0, 5), Is.EqualTo(Cards.Red));
        Assert.That(finished.CellAt(1, 4), Is.EqualTo(Cards.Red));
        Assert.That(finished.CellAt(2, 3), Is.EqualTo(Cards.Red));
        Assert.That(finished.CellAt(3, 2), Is.EqualTo(Cards.Red));
        Assert.That(See.Ids(finished.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row" }));
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
