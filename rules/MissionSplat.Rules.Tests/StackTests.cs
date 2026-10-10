namespace MissionSplat.Rules.Tests;

public class StackTests
{
    private static readonly Cell Stack = Cell.Symbol(OrdinaryCatalog.Stack);

    [Test]
    public void Tiles_ListEachOccupiedPositionOnce_WithTheTopTile_AfterACoverAndAfterABounce()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.BlankTile("pad"),
            Cards.Tile("lid", Stack, Cards.Blank, Cards.Blank, Cards.Blank),
            Cards.Tile("bouncer", Cell.Symbol(OrdinaryCatalog.Bounce), Cards.Blank, Cards.Blank, Cards.Blank));
        Assert.That(game.Tiles, Is.EqualTo(new[] { new VisibleTile(new TileId("start"), 0, 0) }));

        var covered = See.Game(RepresentativeDeck.Play(See.Game(RepresentativeDeck.Play(game, 1, 0)), 0, 0));
        Assert.That(
            covered.Tiles,
            Is.EqualTo(new[] { new VisibleTile(new TileId("lid"), 0, 0), new VisibleTile(new TileId("pad"), 1, 0) }));
        Assert.That(covered.Tiles, Has.Count.EqualTo(covered.TileCount));

        var bounced = See.Game(RepresentativeDeck.Bounce(covered, 0, 0));
        Assert.That(
            bounced.Tiles,
            Is.EqualTo(new[] { new VisibleTile(new TileId("start"), 0, 0), new VisibleTile(new TileId("pad"), 1, 0) }));
    }

    [Test]
    public void Tiles_AreOrderedByXThenY()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.BlankTile("north"),
            Cards.BlankTile("west"),
            Cards.BlankTile("east"));

        var placed = See.Game(RepresentativeDeck.Play(See.Game(RepresentativeDeck.Play(See.Game(RepresentativeDeck.Play(game, 0, 1)), -1, 0)), 1, 0));

        Assert.That(
            placed.Tiles.Select(t => (t.Id.Value, t.TileX, t.TileY)),
            Is.EqualTo(new[] { ("west", -1, 0), ("start", 0, 0), ("north", 0, 1), ("east", 1, 0) }));
    }

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

        var stacked = RepresentativeDeck.Play(ready, 0, 0);
        var next = See.Game(stacked);

        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Blue));
        Assert.That(next.CellAt(1, 0), Is.EqualTo(Cards.Blue));
        Assert.That(next.CellAt(0, 1), Is.EqualTo(Stack));
        Assert.That(next.CellAt(1, 1), Is.EqualTo(Stack));
        Assert.That(next.CellAt(2, 0), Is.EqualTo(Cards.Blue));
        Assert.That(next.CellAt(3, 0), Is.EqualTo(Cards.Blue));
        Assert.That(next.HasTileAt(0, 0), Is.True);
        Assert.That(next.TileCount, Is.EqualTo(3));
        Assert.That(See.Ids(next.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "covered" }));
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
        Assert.That(placed.Covered, Is.EqualTo(new TileId("covered")));
        Assert.That(((MissionClaimed)stacked.Events[1]).Mission.Value, Is.EqualTo("blue-row"));
        Assert.That(stacked.Events.OfType<GameWon>(), Is.Empty);
        Assert.That(ready.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(ready.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(ready.TileCount, Is.EqualTo(3));
    }

    [Test]
    public void ExtraStackCells_ConsumeTheTileOnce_AndPassTheTurnOnce()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile("double", Stack, Stack, Cards.Blank, Cards.Blank),
            Cards.BlankTile("next"));

        var stacked = RepresentativeDeck.Play(game, 0, 0);
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
        Assert.That(See.Ids(next.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "start" }));

        var again = RepresentativeDeck.Try(next, new Place(0, 0, 0));
        Expect.Rejected(next, again, RejectionReason.CellOccupied);
        Assert.That(next.MatchDeckRemaining, Is.EqualTo(1));
    }

    [Test]
    public void SecondStack_KeepsTheEarlierCoveredTile_UnderTheNewTop()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.Solid("bottom", Cards.Red),
            Cards.Tile("middle", Cell.Color(OrdinaryCatalog.Green), Stack, Cards.Blank, Cards.Blank),
            Cards.BlankTile("beside"),
            Cards.Tile("top", Cards.Blue, Stack, Cards.Blank, Cards.Blank));

        var first = RepresentativeDeck.Play(game, 0, 0);
        var coveredOnce = See.Game(first);
        Assert.That(coveredOnce.CellAt(0, 0), Is.EqualTo(Cell.Color(OrdinaryCatalog.Green)));
        Assert.That(See.Ids(coveredOnce.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "bottom" }));
        Assert.That(coveredOnce.TileCount, Is.EqualTo(1));
        Assert.That(coveredOnce.HasTileAt(0, 0), Is.True);
        Assert.That(coveredOnce.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(first.Events, Has.Count.EqualTo(1));

        var occupied = RepresentativeDeck.Try(coveredOnce, new Place(0, 0, 0));
        Expect.Rejected(coveredOnce, occupied, RejectionReason.CellOccupied);

        var afterBeside = See.Game(RepresentativeDeck.Play(coveredOnce, 1, 0));
        Assert.That(See.Ids(afterBeside.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "bottom" }));
        Assert.That(afterBeside.CoveredTileIds(1, 0), Is.Empty);
        Assert.That(afterBeside.TileCount, Is.EqualTo(2));
        Assert.That(afterBeside.CellAt(0, 0), Is.EqualTo(Cell.Color(OrdinaryCatalog.Green)));

        var second = RepresentativeDeck.Play(afterBeside, 0, 0);
        var coveredTwice = See.Game(second);
        Assert.That(See.Ids(coveredTwice.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "bottom", "middle" }));
        Assert.That(See.Ids(coveredOnce.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "bottom" }));
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
        Assert.That(((TilePlaced)second.Events[0]).Covered, Is.EqualTo(new TileId("middle")));
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
        var stacked = RepresentativeDeck.Play(afterAside, 1, 0);
        var next = See.Game(stacked);

        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cell.Color(OrdinaryCatalog.Green)));
        Assert.That(next.CellAt(1, 1), Is.EqualTo(Cell.Color(OrdinaryCatalog.Green)));
        Assert.That(next.CellAt(2, 0), Is.EqualTo(Stack));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "green-square", "a2" }));
        Assert.That(stacked.Events, Has.Count.EqualTo(1));
        Assert.That(stacked.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(See.Ids(next.CoveredTileIds(1, 0)), Is.EqualTo(new[] { "pad" }));
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

        var stacked = RepresentativeDeck.Play(ready, 0, 0);
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
        Assert.That(See.Ids(won.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "covered" }));
        Assert.That(won.CellAt(0, 0), Is.EqualTo(Cards.Blue));
        Assert.That(ready.HasEnded, Is.False);
        Assert.That(ready.PendingMatchTile!.Id.Value, Is.EqualTo("top"));

        var tiles = won.TileCount;
        var rejected = RepresentativeDeck.Try(won, new Place(0, 0, 0));
        Expect.Rejected(won, rejected, RejectionReason.GameOver);
        Assert.That(won.TileCount, Is.EqualTo(tiles));
        Assert.That(won.MatchDeckRemaining, Is.EqualTo(1));
    }

    [TestCase("blank")]
    [TestCase("rotate")]
    [TestCase("bounce")]
    public void PlaceOnAnOccupiedPosition_WithoutStack_IsRejected_AndTheGameStays(string symbol)
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile("plain", Cards.Symbol(symbol), Cards.Blank, Cards.Blank, Cards.Blank));

        Assert.That(() => game.CoveredTileIds(4, -3), Throws.Nothing);
        Assert.That(game.CoveredTileIds(0, 0), Is.Empty);
        Assert.That(game.CoveredTileIds(4, -3), Is.Empty);
        Assert.That(game.HasTileAt(0, 0), Is.True);

        var rejected = RepresentativeDeck.Try(game, new Place(0, 0, 0));
        Expect.Rejected(game, rejected, RejectionReason.CellOccupied);
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Blank));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
    }

    [Test]
    public void StackTileAtAnEmptyPosition_PlacesBesideLikeAnyTile_AndCoversNothing()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile("stacked", Stack, Cards.Blank, Cards.Blank, Cards.Blank),
            Cards.Tile("stacked-again", Stack, Cards.Blank, Cards.Blank, Cards.Blank));

        var apart = RepresentativeDeck.Try(game, new Place(3, 0, 0));
        Expect.Rejected(game, apart, RejectionReason.DoesNotShareFullSide);

        var beside = RepresentativeDeck.Play(game, 1, 0);
        var next = See.Game(beside);

        Assert.That(((TilePlaced)beside.Events.Single()).Covered, Is.Null);
        Assert.That(next.TileCount, Is.EqualTo(2));
        Assert.That(next.CoveredTileIds(0, 0), Is.Empty);
        Assert.That(next.CoveredTileIds(1, 0), Is.Empty);
        Assert.That(next.CellAt(2, 0), Is.EqualTo(Stack));
        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Blank));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void QuarterTurnsClockwise_MoveTheTopCells(int quarterTurns)
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile("spun", Cards.Red, Cards.Blue, Cell.Color(OrdinaryCatalog.Green), Stack));

        var stacked = RepresentativeDeck.Play(game, 0, 0, quarterTurns);
        var placed = See.Game(stacked);

        Expect.Tile(
            placed,
            0,
            0,
            Oriented.Clockwise([Cards.Red, Cards.Blue, Cell.Color(OrdinaryCatalog.Green), Stack], quarterTurns));
        Assert.That(placed.TileCount, Is.EqualTo(1));
        Assert.That(See.Ids(placed.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "start" }));
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
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile("stacked", Stack, Cards.Blank, Cards.Blank, Cards.Blank));

        CommandResult? result = null;
        Assert.That(() => { result = RepresentativeDeck.Try(game, new Place(0, 0, quarterTurns)); }, Throws.Nothing);

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
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"));
        var handA = See.Ids(game.Hand(Cards.Seat("a")));
        var handB = See.Ids(game.Hand(Cards.Seat("b")));

        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(
            () => { RepresentativeDeck.Try(game, new Place(0, 0, 0)); },
            Throws.TypeOf<UnresolvedRulingException>().With.Message.EqualTo(
                "The match deck has no tile to place. Exhausting the match deck is an open ruling, so this command was not applied."));

        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(See.Ids(game.Hand(Cards.Seat("a"))), Is.EqualTo(handA));
        Assert.That(See.Ids(game.Hand(Cards.Seat("b"))), Is.EqualTo(handB));
        Assert.That(game.CoveredTileIds(0, 0), Is.Empty);
    }

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

}
