namespace MissionSplat.Rules.Tests;

public class StackTests
{
    private static readonly Cell Stack = Cell.Symbol(OrdinaryCatalog.Stack);

    [Test]
    public void CoveredColor_NoLongerCompletes_AndTopColorCompletesTheActingMission()
    {
        // Representative deck. The start tile is a red square. The bridge holds the other half of a blue row.
        // Covering the start removes that red square and writes the blue half, so only the acting seat's blue row completes.
        var top = Cards.Tile("top", Cards.Blue, Cards.Blue, Stack, Stack);
        var ready = BeforeCoveringRedSquare(
            top,
            [
                Cards.Mission("red-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Mission("blue-row", MissionPattern.Row, OrdinaryCatalog.Blue),
                Cards.Mission("b-blue", MissionPattern.Row, OrdinaryCatalog.Blue),
                Cards.Purple("b2"),
                Cards.Purple("repl"),
                Cards.Purple("spare"),
            ],
            OrdinaryCatalog.ClaimsRequiredToWin,
            []);

        Assert.That(ready.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(ready.CellAt(1, 1), Is.EqualTo(Cards.Red));
        Assert.That(ready.CellAt(2, 0), Is.EqualTo(Cards.Blue));
        Assert.That(ready.CellAt(3, 0), Is.EqualTo(Cards.Blue));
        Assert.That(ready.CoveredTileIds(0, 0), Is.Empty);
        Assert.That(ready.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(ready.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(ready.PendingMatchTile!.Id.Value, Is.EqualTo("top"));
        Assert.That(ready.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(ready.TileCount, Is.EqualTo(3));

        var occupied = ready.Place(0, 0, 0);
        AssertRejected(ready, occupied, RejectionReason.CellOccupied);
        Assert.That(ready.MatchDeckRemaining, Is.EqualTo(1));

        var stacked = RepresentativeDeck.Stack(ready, 0, 0);
        var next = See.Game(stacked);

        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Blue));
        Assert.That(next.CellAt(1, 0), Is.EqualTo(Cards.Blue));
        Assert.That(next.CellAt(0, 1), Is.EqualTo(Stack));
        Assert.That(next.CellAt(1, 1), Is.EqualTo(Stack));
        Assert.That(next.CellAt(2, 0), Is.EqualTo(Cards.Blue));
        Assert.That(next.CellAt(3, 0), Is.EqualTo(Cards.Blue));
        Assert.That(next.HasTileAt(0, 0), Is.True);
        Assert.That(next.TileCount, Is.EqualTo(3));
        Assert.That(Ids(next.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "covered" }));
        Assert.That(next.CoveredTileIds(1, 0), Is.Empty);
        Assert.That(next.CoveredTileIds(0, 1), Is.Empty);
        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "blue-row" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "red-square", "repl" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("b"))), Is.EqualTo(new[] { "b-blue", "b2" }));
        Assert.That(next.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(next.HasEnded, Is.False);
        Assert.That(next.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(stacked.Events, Has.Count.EqualTo(2));
        Assert.That(stacked.Events[0], Is.TypeOf<TilePlaced>());
        var placed = (TilePlaced)stacked.Events[0];
        Assert.That(placed.Seat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(placed.Tile.Value, Is.EqualTo("top"));
        Assert.That(placed.TileX, Is.EqualTo(0));
        Assert.That(placed.TileY, Is.EqualTo(0));
        Assert.That(placed.QuarterTurnsClockwise, Is.EqualTo(0));
        Assert.That(((MissionClaimed)stacked.Events[1]).Mission.Value, Is.EqualTo("blue-row"));
        Assert.That(stacked.Events.OfType<GameWon>(), Is.Empty);
        Assert.That(ready.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(ready.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(ready.TileCount, Is.EqualTo(3));
    }

    [Test]
    public void ExtraStackCells_ConsumeTheTileOnce_AndPassTheTurnOnce()
    {
        var game = TwoSeatGame(
            Cards.BlankTile("start"),
            Cards.Tile("double", Stack, Stack, Cards.Blank, Cards.Blank),
            Cards.BlankTile("next"));

        var stacked = RepresentativeDeck.Stack(game, 0, 0);
        var next = See.Game(stacked);

        Assert.That(stacked.Events, Has.Count.EqualTo(1));
        Assert.That(stacked.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(next.CellAt(0, 0), Is.EqualTo(Stack));
        Assert.That(next.CellAt(1, 0), Is.EqualTo(Stack));
        Assert.That(next.TileCount, Is.EqualTo(1));
        Assert.That(next.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(next.PendingMatchTile!.Id.Value, Is.EqualTo("next"));
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(Ids(next.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "start" }));

        var again = next.Stack(0, 0, 0);
        AssertRejected(next, again, RejectionReason.NoStackCell);
        Assert.That(next.MatchDeckRemaining, Is.EqualTo(1));
    }

    [Test]
    public void SecondStack_KeepsTheEarlierCoveredTile_UnderTheNewTop()
    {
        var game = TwoSeatGame(
            Cards.Solid("bottom", Cards.Red),
            Cards.Tile("middle", Cell.Color(OrdinaryCatalog.Green), Stack, Cards.Blank, Cards.Blank),
            Cards.BlankTile("beside"),
            Cards.Tile("top", Cards.Blue, Stack, Cards.Blank, Cards.Blank));

        var first = RepresentativeDeck.Stack(game, 0, 0);
        var coveredOnce = See.Game(first);
        Assert.That(coveredOnce.CellAt(0, 0), Is.EqualTo(Cell.Color(OrdinaryCatalog.Green)));
        Assert.That(Ids(coveredOnce.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "bottom" }));
        Assert.That(coveredOnce.TileCount, Is.EqualTo(1));
        Assert.That(coveredOnce.HasTileAt(0, 0), Is.True);
        Assert.That(coveredOnce.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(first.Events, Has.Count.EqualTo(1));

        var occupied = coveredOnce.Place(0, 0, 0);
        AssertRejected(coveredOnce, occupied, RejectionReason.CellOccupied);

        var afterBeside = See.Game(RepresentativeDeck.Play(coveredOnce, 1, 0));
        Assert.That(Ids(afterBeside.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "bottom" }));
        Assert.That(afterBeside.CoveredTileIds(1, 0), Is.Empty);
        Assert.That(afterBeside.TileCount, Is.EqualTo(2));
        Assert.That(afterBeside.CellAt(0, 0), Is.EqualTo(Cell.Color(OrdinaryCatalog.Green)));

        var stillOccupied = afterBeside.Place(0, 0, 0);
        AssertRejected(afterBeside, stillOccupied, RejectionReason.CellOccupied);

        var second = RepresentativeDeck.Stack(afterBeside, 0, 0);
        var coveredTwice = See.Game(second);
        Assert.That(Ids(coveredTwice.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "bottom", "middle" }));
        Assert.That(Ids(coveredOnce.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "bottom" }));
        Assert.That(coveredTwice.CellAt(0, 0), Is.EqualTo(Cards.Blue));
        Assert.That(coveredTwice.CellAt(1, 0), Is.EqualTo(Stack));
        Assert.That(coveredTwice.TileCount, Is.EqualTo(2));
        Assert.That(coveredTwice.HasTileAt(0, 0), Is.True);
        Assert.That(coveredTwice.HasTileAt(1, 0), Is.True);
        Assert.That(coveredTwice.CoveredTileIds(1, 0), Is.Empty);
        Assert.That(coveredTwice.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(coveredTwice.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(second.Events, Has.Count.EqualTo(1));
        Assert.That(((TilePlaced)second.Events[0]).Tile.Value, Is.EqualTo("top"));
    }

    [Test]
    public void PatternThatMissesTheWrittenCells_IsNotClaimed()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("green-square", MissionPattern.Square, OrdinaryCatalog.Green),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("spare"),
            ],
            [
                Cards.Solid("origin", Cell.Color(OrdinaryCatalog.Green)),
                Cards.BlankTile("pad"),
                Cards.BlankTile("aside"),
                Cards.Tile("cover", Stack, Cards.Blank, Cards.Blank, Cards.Blank),
            ]);

        var afterPad = See.Game(RepresentativeDeck.Play(game, 1, 0));
        Assert.That(afterPad.Claims(Cards.Seat("a")), Is.Empty);

        var afterAside = See.Game(RepresentativeDeck.Play(afterPad, -1, 0));
        var stacked = RepresentativeDeck.Stack(afterAside, 1, 0);
        var next = See.Game(stacked);

        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cell.Color(OrdinaryCatalog.Green)));
        Assert.That(next.CellAt(1, 1), Is.EqualTo(Cell.Color(OrdinaryCatalog.Green)));
        Assert.That(next.CellAt(2, 0), Is.EqualTo(Stack));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "green-square", "a2" }));
        Assert.That(stacked.Events, Has.Count.EqualTo(1));
        Assert.That(stacked.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(Ids(next.CoveredTileIds(1, 0)), Is.EqualTo(new[] { "pad" }));
        Assert.That(next.CoveredTileIds(0, 0), Is.Empty);
        Assert.That(next.TileCount, Is.EqualTo(3));
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
    }

    [Test]
    public void StackThatMeetsTheWinCount_EmitsWonAfterTheClaim_AndRejectsTheNextStack()
    {
        var later = Cards.Tile("later", Stack, Cards.Blank, Cards.Blank, Cards.Blank);
        var ready = BeforeCoveringRedSquare(
            Cards.Tile("top", Cards.Blue, Cards.Blue, Stack, Cards.Blank),
            [
                Cards.Mission("blue-row", MissionPattern.Row, OrdinaryCatalog.Blue),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("repl"),
                Cards.Purple("spare"),
            ],
            1,
            [later]);

        var stacked = RepresentativeDeck.Stack(ready, 0, 0);
        var won = See.Game(stacked);

        Assert.That(stacked.Events, Has.Count.EqualTo(3));
        Assert.That(stacked.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(((MissionClaimed)stacked.Events[1]).Mission.Value, Is.EqualTo("blue-row"));
        var gameWon = (GameWon)stacked.Events[2];
        Assert.That(gameWon.Seat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(gameWon.ClaimCount, Is.EqualTo(1));
        Assert.That(won.HasEnded, Is.True);
        Assert.That(won.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(won.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(won.PendingMatchTile, Is.Null);
        Assert.That(Ids(won.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "covered" }));
        Assert.That(won.CellAt(0, 0), Is.EqualTo(Cards.Blue));
        Assert.That(ready.HasEnded, Is.False);
        Assert.That(ready.PendingMatchTile!.Id.Value, Is.EqualTo("top"));

        var tiles = won.TileCount;
        var rejected = won.Stack(0, 0, 0);
        AssertRejected(won, rejected, RejectionReason.GameOver);
        Assert.That(won.TileCount, Is.EqualTo(tiles));
        Assert.That(won.MatchDeckRemaining, Is.EqualTo(1));
    }

    [TestCase("blank")]
    [TestCase("rotate")]
    [TestCase("bounce")]
    public void DrawnTileWithoutStack_IsRejected_AndTheGameStays(string symbol)
    {
        var game = TwoSeatGame(
            Cards.BlankTile("start"),
            Cards.Tile("plain", Cards.Symbol(symbol), Cards.Blank, Cards.Blank, Cards.Blank));

        Assert.That(() => game.CoveredTileIds(4, -3), Throws.Nothing);
        Assert.That(game.CoveredTileIds(0, 0), Is.Empty);
        Assert.That(game.CoveredTileIds(4, -3), Is.Empty);
        Assert.That(game.HasTileAt(0, 0), Is.True);

        var rejected = game.Stack(0, 0, 0);
        AssertRejected(game, rejected, RejectionReason.NoStackCell);
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Blank));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
    }

    [Test]
    public void EmptyPosition_IsRejected_EvenWhenThatSquareSharesAFullSide()
    {
        var game = TwoSeatGame(
            Cards.BlankTile("start"),
            Cards.Tile("stacked", Stack, Cards.Blank, Cards.Blank, Cards.Blank));

        var empty = game.Stack(1, 0, 0);
        AssertRejected(game, empty, RejectionReason.NoTileToCover);
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.HasTileAt(1, 0), Is.False);
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));

        var covered = See.Game(RepresentativeDeck.Stack(game, 0, 0));
        Assert.That(covered.TileCount, Is.EqualTo(1));
        Assert.That(covered.HasTileAt(0, 0), Is.True);
        Assert.That(covered.CellAt(0, 0), Is.EqualTo(Stack));
        Assert.That(Ids(covered.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "start" }));
        Assert.That(covered.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void QuarterTurnsClockwise_MoveTheTopCells(int quarterTurns)
    {
        var game = TwoSeatGame(
            Cards.BlankTile("start"),
            Cards.Tile("spun", Cards.Red, Cards.Blue, Cell.Color(OrdinaryCatalog.Green), Stack));

        var stacked = RepresentativeDeck.Stack(game, 0, 0, quarterTurns);
        var placed = See.Game(stacked);
        var expected = ExpectedClockwise(quarterTurns);

        Assert.That(placed.CellAt(0, 0), Is.EqualTo(expected[0]));
        Assert.That(placed.CellAt(1, 0), Is.EqualTo(expected[1]));
        Assert.That(placed.CellAt(0, 1), Is.EqualTo(expected[2]));
        Assert.That(placed.CellAt(1, 1), Is.EqualTo(expected[3]));
        Assert.That(placed.TileCount, Is.EqualTo(1));
        Assert.That(Ids(placed.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "start" }));
        Assert.That(placed.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        var turn = (TilePlaced)stacked.Events[0];
        Assert.That(turn.QuarterTurnsClockwise, Is.EqualTo(quarterTurns));
        Assert.That(turn.TileX, Is.EqualTo(0));
        Assert.That(turn.TileY, Is.EqualTo(0));
    }

    [TestCase(4)]
    [TestCase(-1)]
    public void QuarterTurnsOutsideZeroThroughThree_AreRejected(int quarterTurns)
    {
        var game = TwoSeatGame(
            Cards.BlankTile("start"),
            Cards.Tile("stacked", Stack, Cards.Blank, Cards.Blank, Cards.Blank));

        CommandResult? result = null;
        Assert.That(() => { result = game.Stack(0, 0, quarterTurns); }, Throws.Nothing);

        Assert.That(result?.IsAccepted, Is.False);
        Assert.That(result?.Rejection?.Reason, Is.EqualTo(RejectionReason.InvalidQuarterTurns));
        Assert.That(result?.Events, Is.Empty);
        Assert.That(result?.Game, Is.SameAs(game));
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
    }

    [Test]
    public void EmptyMatchDeck_ThrowsUnresolvedRuling_AndLeavesTheGame()
    {
        var game = TwoSeatGame(Cards.BlankTile("start"));
        var handA = See.Ids(game.Hand(Cards.Seat("a")));
        var handB = See.Ids(game.Hand(Cards.Seat("b")));

        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(
            () => { game.Stack(0, 0, 0); },
            Throws.TypeOf<UnresolvedRulingException>().With.Message.EqualTo(
                "The match deck has no tile to place. Exhausting the match deck is an open ruling, so this command was not applied."));

        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(See.Ids(game.Hand(Cards.Seat("a"))), Is.EqualTo(handA));
        Assert.That(See.Ids(game.Hand(Cards.Seat("b"))), Is.EqualTo(handB));
        Assert.That(game.CoveredTileIds(0, 0), Is.Empty);
    }

    // Y increases upward. One clockwise turn moves bottom-left red to top-left.
    private static Cell[] ExpectedClockwise(int quarterTurns) => quarterTurns switch
    {
        0 => [Cards.Red, Cards.Blue, Cell.Color(OrdinaryCatalog.Green), Stack],
        1 => [Cards.Blue, Stack, Cards.Red, Cell.Color(OrdinaryCatalog.Green)],
        2 => [Stack, Cell.Color(OrdinaryCatalog.Green), Cards.Blue, Cards.Red],
        3 => [Cell.Color(OrdinaryCatalog.Green), Cards.Red, Stack, Cards.Blue],
        _ => throw new ArgumentOutOfRangeException(nameof(quarterTurns)),
    };

    private static Game BeforeCoveringRedSquare(
        Tile stackTile,
        Mission[] missions,
        int claimsRequiredToWin,
        Tile[] following)
    {
        var tiles = new List<Tile>
        {
            Cards.Solid("covered", Cards.Red),
            Cards.Tile("bridge", Cards.Blue, Cards.Blue, Cards.Blank, Cards.Blank),
            Cards.BlankTile("aside"),
            stackTile,
        };
        tiles.AddRange(following);
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            missions,
            tiles.ToArray(),
            claimsRequiredToWin);
        var afterBridge = See.Game(RepresentativeDeck.Play(game, 1, 0));
        return See.Game(RepresentativeDeck.Play(afterBridge, 0, 1));
    }

    private static void AssertRejected(Game game, CommandResult result, RejectionReason reason)
    {
        Assert.That(result.IsAccepted, Is.False);
        Assert.That(result.Rejection?.Reason, Is.EqualTo(reason));
        Assert.That(result.Events, Is.Empty);
        Assert.That(result.Game, Is.SameAs(game));
    }

    private static string[] Ids(IReadOnlyList<TileId> tiles) =>
        tiles.Select(tile => tile.Value).ToArray();

    private static Game TwoSeatGame(params Tile[] tiles)
    {
        return RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
            tiles);
    }
}
