namespace MissionSplat.Rules.Tests;

public class RotateTests
{
    private static readonly Cell Rotate = Cell.Symbol(OrdinaryCatalog.Rotate);

    private static readonly Cell Stack = Cell.Symbol(OrdinaryCatalog.Stack);

    private static readonly Cell Green = Cell.Color(OrdinaryCatalog.Green);

    private static readonly Cell Purple = Cell.Color(OrdinaryCatalog.Purple);

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void FreeSide_TurnsOnlyThatTilesCells(int quarterTurns)
    {
        var game = TwoSeatGame(Cross("start"), OneRotate("drawn"));

        var result = RepresentativeDeck.Rotate(game, 1, 0, [new RotateUse(0, 0, quarterTurns)]);
        var next = See.Game(result);

        AssertTile(next, 0, 0, ExpectedClockwise(quarterTurns));
        AssertTile(next, 1, 0, [Rotate, Cards.Blank, Cards.Blank, Cards.Blank]);
        Assert.That(next.CellAt(4, 4), Is.Null);
        Assert.That(next.TileCount, Is.EqualTo(2));
        Assert.That(next.HasTileAt(0, 0), Is.True);
        Assert.That(next.HasTileAt(1, 0), Is.True);
        Assert.That(next.CoveredTileIds(0, 0), Is.Empty);
        Assert.That(next.CoveredTileIds(1, 0), Is.Empty);
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(next.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(result.Events, Has.Count.EqualTo(1));
        var placed = (TilePlaced)result.Events[0];
        Assert.That(placed.Seat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(placed.Tile.Value, Is.EqualTo("drawn"));
        Assert.That(placed.TileX, Is.EqualTo(1));
        Assert.That(placed.TileY, Is.EqualTo(0));
        Assert.That(placed.QuarterTurnsClockwise, Is.EqualTo(0));
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
    }

    [Test]
    public void PlacementQuarterTurns_AndALaterRotate_ComposeClockwise()
    {
        var spun = Cards.Tile("spun", Rotate, Cards.Red, Cards.Blue, Green);
        var game = TwoSeatGame(Cards.BlankTile("start"), spun);

        var oriented = See.Game(RepresentativeDeck.Play(game, 1, 0, 2));
        var rotated = RepresentativeDeck.Rotate(game, 1, 0, [new RotateUse(1, 0, 1)], 1);
        var composed = See.Game(rotated);

        AssertTile(oriented, 1, 0, [Green, Cards.Blue, Cards.Red, Rotate]);
        AssertTile(composed, 1, 0, [Green, Cards.Blue, Cards.Red, Rotate]);
        AssertTile(composed, 0, 0, [Cards.Blank, Cards.Blank, Cards.Blank, Cards.Blank]);
        Assert.That(composed.TileCount, Is.EqualTo(2));
        Assert.That(((TilePlaced)rotated.Events[0]).QuarterTurnsClockwise, Is.EqualTo(1));
    }

    [Test]
    public void FourOrthogonalNeighbors_RejectTheRotate_AndLeaveTheGame()
    {
        var game = SurroundedCenter();

        var rejected = game.PlaceWithRotates(2, 0, 0, [new RotateUse(0, 0, 1)]);

        AssertRejected(game, rejected, RejectionReason.TileSurrounded);
        Assert.That(game.TileCount, Is.EqualTo(5));
        Assert.That(game.HasTileAt(2, 0), Is.False);
        Assert.That(game.HasTileAt(0, -1), Is.True);
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(game.PendingMatchTile!.Id.Value, Is.EqualTo("drawn"));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
    }

    [Test]
    public void PlacementThatFillsTheFourthSide_RejectsRotateOfThatTile()
    {
        var game = CenterWithAnOpenSouth();

        var rejected = game.PlaceWithRotates(0, -1, 0, [new RotateUse(0, 0, 1)]);

        AssertRejected(game, rejected, RejectionReason.TileSurrounded);
        Assert.That(game.TileCount, Is.EqualTo(4));
        Assert.That(game.HasTileAt(0, -1), Is.False);
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(game.CellAt(0, 2), Is.EqualTo(Cards.Red));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
    }

    [Test]
    public void OpenSide_StillTurns_WhenThePlacementSurroundsADifferentTile()
    {
        var game = CenterWithAnOpenSouth();

        var result = RepresentativeDeck.Rotate(game, 0, -1, [new RotateUse(0, 1, 1)]);
        var next = See.Game(result);

        AssertTile(next, 0, 1, ExpectedClockwise(1));
        AssertTile(next, 0, 0, ExpectedClockwise(0));
        Assert.That(next.CellAt(0, -2), Is.EqualTo(Rotate));
        Assert.That(next.TileCount, Is.EqualTo(5));
        Assert.That(next.HasTileAt(0, -1), Is.True);
        Assert.That(next.CoveredTileIds(0, 1), Is.Empty);
    }

    [Test]
    public void DiagonalNeighbor_DoesNotCountAsSurrounded()
    {
        var game = TwoSeatGame(
            Cross("center"),
            Cards.BlankTile("east"),
            Cards.BlankTile("diagonal"),
            Cards.BlankTile("north"),
            Cards.BlankTile("west"),
            OneRotate("drawn"));
        foreach (var (x, y) in new[] { (1, 0), (1, 1), (0, 1), (-1, 0) })
        {
            game = See.Game(RepresentativeDeck.Play(game, x, y));
        }

        var result = RepresentativeDeck.Rotate(game, 2, 0, [new RotateUse(0, 0, 1)]);
        var next = See.Game(result);

        AssertTile(next, 0, 0, ExpectedClockwise(1));
        Assert.That(next.CellAt(2, 0), Is.EqualTo(Cards.Blank));
        Assert.That(next.CellAt(2, 2), Is.EqualTo(Cards.Blank));
        Assert.That(next.CellAt(4, 0), Is.EqualTo(Rotate));
        Assert.That(next.HasTileAt(1, 1), Is.True);
        Assert.That(next.HasTileAt(0, -1), Is.False);
        Assert.That(next.TileCount, Is.EqualTo(6));
    }

    [Test]
    public void CoveredStack_StillSurrounds_AndItsVisibleCellsTurnWithoutTheBuriedTile()
    {
        var buried = TwoSeatGame(
            Cross("buried"),
            Cards.Tile("top", Cards.Red, Cards.Blue, Green, Stack),
            OneRotate("drawn"));
        var covered = See.Game(RepresentativeDeck.Stack(buried, 0, 0));

        var turned = RepresentativeDeck.Rotate(covered, 1, 0, [new RotateUse(0, 0, 1)]);
        var next = See.Game(turned);

        AssertTile(next, 0, 0, [Cards.Blue, Stack, Cards.Red, Green]);
        AssertTile(next, 1, 0, [Rotate, Cards.Blank, Cards.Blank, Cards.Blank]);
        Assert.That(Ids(next.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "buried" }));
        Assert.That(Ids(covered.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "buried" }));
        Assert.That(next.TileCount, Is.EqualTo(2));
        Assert.That(next.HasTileAt(0, 0), Is.True);

        var blocked = StackedEastSurroundsCenter();
        var rejected = blocked.PlaceWithRotates(2, 0, 0, [new RotateUse(0, 0, 1)]);

        AssertRejected(blocked, rejected, RejectionReason.TileSurrounded);
        Assert.That(Ids(blocked.CoveredTileIds(1, 0)), Is.EqualTo(new[] { "east" }));
        Assert.That(blocked.CellAt(2, 0), Is.EqualTo(Stack));
        Assert.That(blocked.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(blocked.TileCount, Is.EqualTo(5));
        Assert.That(blocked.HasTileAt(1, 0), Is.True);
        Assert.That(blocked.HasTileAt(2, 0), Is.False);
    }

    [Test]
    public void TwoRotateCells_TurnTwoTilesOrTheSameTileTwice_AndAThirdUseIsRejected()
    {
        var drawn = Cards.Tile("drawn", Rotate, Rotate, Cards.Red, Cards.Blue);
        var game = TwoSeatGame(Cross("start"), drawn);
        var third = game.PlaceWithRotates(
            1,
            0,
            0,
            [new RotateUse(0, 0, 1), new RotateUse(1, 0, 1), new RotateUse(0, 0, 1)]);
        AssertRejected(game, third, RejectionReason.TooManyRotateUses);
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));

        var both = See.Game(RepresentativeDeck.Rotate(
            game,
            1,
            0,
            [new RotateUse(0, 0, 1), new RotateUse(1, 0, 2)]));
        AssertTile(both, 0, 0, ExpectedClockwise(1));
        AssertTile(both, 1, 0, [Cards.Blue, Cards.Red, Rotate, Rotate]);
        Assert.That(both.TileCount, Is.EqualTo(2));
        Assert.That(both.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(both.Claims(Cards.Seat("a")), Is.Empty);

        var twice = See.Game(RepresentativeDeck.Rotate(
            game,
            1,
            0,
            [new RotateUse(1, 0, 1), new RotateUse(1, 0, 1)]));
        AssertTile(twice, 0, 0, ExpectedClockwise(0));
        AssertTile(twice, 1, 0, [Cards.Blue, Cards.Red, Rotate, Rotate]);
        Assert.That(twice.TileCount, Is.EqualTo(2));
    }

    [Test]
    public void RotateSymbolAlreadyOnTheBoard_AddsNoUse()
    {
        var plain = TwoSeatGame(
            Cards.Tile("start", Rotate, Cards.Red, Cards.Blue, Cards.Blank),
            Cards.BlankTile("drawn"));
        var blocked = plain.PlaceWithRotates(1, 0, 0, [new RotateUse(0, 0, 1)]);

        AssertRejected(plain, blocked, RejectionReason.NoRotateCell);
        Assert.That(plain.CellAt(0, 0), Is.EqualTo(Rotate));
        Assert.That(plain.TileCount, Is.EqualTo(1));
        Assert.That(plain.MatchDeckRemaining, Is.EqualTo(1));

        var charged = TwoSeatGame(
            Cards.Tile("start", Rotate, Cards.Red, Cards.Blue, Cards.Blank),
            OneRotate("drawn"));
        var extra = charged.PlaceWithRotates(1, 0, 0, [new RotateUse(0, 0, 1), new RotateUse(1, 0, 1)]);
        AssertRejected(charged, extra, RejectionReason.TooManyRotateUses);

        var once = See.Game(RepresentativeDeck.Rotate(charged, 1, 0, [new RotateUse(0, 0, 1)]));
        AssertTile(once, 0, 0, [Cards.Red, Cards.Blank, Rotate, Cards.Blue]);
        AssertTile(once, 1, 0, [Rotate, Cards.Blank, Cards.Blank, Cards.Blank]);
        Assert.That(once.TileCount, Is.EqualTo(2));
    }

    [Test]
    public void Place_DoesNotRotate_AndDoesNotClaimTheUnturnedRow()
    {
        var game = ReadyToFinishRedRow(OrdinaryCatalog.ClaimsRequiredToWin);

        var placed = RepresentativeDeck.Play(game, 1, 0);
        var next = See.Game(placed);

        Assert.That(next.CellAt(2, 0), Is.EqualTo(Rotate));
        Assert.That(next.CellAt(3, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(2, 1), Is.EqualTo(Cards.Blank));
        Assert.That(next.CellAt(3, 1), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row", "a2" }));
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(placed.Events, Has.Count.EqualTo(1));
        Assert.That(placed.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(game.PendingMatchTile!.Id.Value, Is.EqualTo("finisher"));
    }

    [Test]
    public void RotatingThePlacedTile_ClaimsFromThoseCellsAfterTheTurn()
    {
        var game = ReadyToFinishRedRow(OrdinaryCatalog.ClaimsRequiredToWin);

        var result = RepresentativeDeck.Rotate(game, 1, 0, [new RotateUse(1, 0, 1)]);
        var next = See.Game(result);

        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(1, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(0, 1), Is.EqualTo(Cards.Blank));
        Assert.That(next.CellAt(2, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(3, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(2, 1), Is.EqualTo(Rotate));
        Assert.That(next.CellAt(3, 1), Is.EqualTo(Cards.Blank));
        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "a2", "repl" }));
        Assert.That(next.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(next.HasEnded, Is.False);
        Assert.That(next.TileCount, Is.EqualTo(2));
        Assert.That(result.Events, Has.Count.EqualTo(2));
        Assert.That(result.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(((TilePlaced)result.Events[0]).QuarterTurnsClockwise, Is.EqualTo(0));
        Assert.That(((MissionClaimed)result.Events[1]).Mission.Value, Is.EqualTo("red-row"));
        Assert.That(result.Events.OfType<GameWon>(), Is.Empty);
        Assert.That(game.CellAt(2, 0), Is.Null);
        Assert.That(game.Claims(Cards.Seat("a")), Is.Empty);
    }

    [Test]
    public void RotatingThePlacedTile_ThatMeetsTheWinCount_EmitsWonAfterTheClaim()
    {
        var game = ReadyToFinishRedRow(1, Cards.BlankTile("later"));

        var result = RepresentativeDeck.Rotate(game, 1, 0, [new RotateUse(1, 0, 1)]);
        var won = See.Game(result);

        Assert.That(result.Events, Has.Count.EqualTo(3));
        Assert.That(result.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(((MissionClaimed)result.Events[1]).Mission.Value, Is.EqualTo("red-row"));
        var gameWon = (GameWon)result.Events[2];
        Assert.That(gameWon.Seat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(gameWon.ClaimCount, Is.EqualTo(1));
        Assert.That(won.HasEnded, Is.True);
        Assert.That(won.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(won.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(won.PendingMatchTile, Is.Null);
        Assert.That(game.HasEnded, Is.False);

        var rejected = won.PlaceWithRotates(2, 0, 0, [new RotateUse(0, 0, 1)]);
        AssertRejected(won, rejected, RejectionReason.GameOver);
        Assert.That(won.TileCount, Is.EqualTo(2));
        Assert.That(won.MatchDeckRemaining, Is.EqualTo(1));
    }

    [Test]
    public void RotatingAnotherTile_DoesNotClaimARowThatMissesThePlacedCells()
    {
        // East completes the red row only after it turns. The written cells are the tile placed on the other side.
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
                Cards.Tile("east", Cards.Blank, Cards.Red, Cards.Blank, Cards.Red),
                Cards.BlankTile("pad"),
                OneRotate("drawn"),
            ]);
        var afterEast = See.Game(RepresentativeDeck.Play(game, 1, 0));
        var ready = See.Game(RepresentativeDeck.Play(afterEast, 0, 1));

        var result = RepresentativeDeck.Rotate(ready, -1, 0, [new RotateUse(1, 0, 1)]);
        var next = See.Game(result);

        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(1, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(2, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(3, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(2, 1), Is.EqualTo(Cards.Blank));
        Assert.That(next.CellAt(3, 1), Is.EqualTo(Cards.Blank));
        Assert.That(next.CellAt(-2, 0), Is.EqualTo(Rotate));
        Assert.That(next.CellAt(0, 2), Is.EqualTo(Cards.Blank));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(next.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row", "a2" }));
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(result.Events, Has.Count.EqualTo(1));
        Assert.That(result.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(next.TileCount, Is.EqualTo(4));
    }

    [Test]
    public void Stack_DoesNotRotate_AndASidePlacementOfTheSameTileCan()
    {
        var both = Cards.Tile("both", Stack, Rotate, Cards.Red, Cards.Blue);
        var game = TwoSeatGame(Cross("start"), both);

        var occupied = game.PlaceWithRotates(0, 0, 0, [new RotateUse(0, 0, 1)]);
        AssertRejected(game, occupied, RejectionReason.CellOccupied);

        var stacked = RepresentativeDeck.Stack(game, 0, 0);
        var covered = See.Game(stacked);
        AssertTile(covered, 0, 0, [Stack, Rotate, Cards.Red, Cards.Blue]);
        Assert.That(Ids(covered.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "start" }));
        Assert.That(covered.TileCount, Is.EqualTo(1));
        Assert.That(covered.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(stacked.Events, Has.Count.EqualTo(1));

        var sided = See.Game(RepresentativeDeck.Rotate(game, 1, 0, [new RotateUse(0, 0, 1)]));
        AssertTile(sided, 0, 0, ExpectedClockwise(1));
        AssertTile(sided, 1, 0, [Stack, Rotate, Cards.Red, Cards.Blue]);
        Assert.That(sided.CoveredTileIds(0, 0), Is.Empty);
        Assert.That(sided.CoveredTileIds(1, 0), Is.Empty);
        Assert.That(sided.TileCount, Is.EqualTo(2));
        Assert.That(sided.HasTileAt(0, 0), Is.True);
        Assert.That(sided.HasTileAt(1, 0), Is.True);
    }

    [Test]
    public void NoUse_IsRejected_EvenWhenTheDrawnTileHasARotateCell()
    {
        var game = TwoSeatGame(Cross("start"), OneRotate("drawn"));

        var rejected = game.PlaceWithRotates(1, 0, 0, []);

        AssertRejected(game, rejected, RejectionReason.NoRotateUse);
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
    }

    [TestCase(0)]
    [TestCase(4)]
    [TestCase(-1)]
    public void RotateUseOutsideOneThroughThree_IsRejected(int quarterTurns)
    {
        var game = TwoSeatGame(Cross("start"), OneRotate("drawn"));

        var rejected = game.PlaceWithRotates(1, 0, 0, [new RotateUse(0, 0, quarterTurns)]);

        AssertRejected(game, rejected, RejectionReason.InvalidRotateQuarterTurns);
        Assert.That(rejected.Rejection?.Message, Is.EqualTo("A rotate use is 1, 2, or 3 quarter-turns."));
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(game.TileCount, Is.EqualTo(1));
    }

    [Test]
    public void EmptyTarget_IsRejected()
    {
        var game = TwoSeatGame(Cross("start"), OneRotate("drawn"));

        var rejected = game.PlaceWithRotates(1, 0, 0, [new RotateUse(3, 3, 1)]);

        AssertRejected(game, rejected, RejectionReason.NoTileToRotate);
        Assert.That(game.HasTileAt(1, 0), Is.False);
        Assert.That(game.HasTileAt(3, 3), Is.False);
        Assert.That(game.TileCount, Is.EqualTo(1));
    }

    [Test]
    public void IllegalLaterUse_DoesNotApplyTheEarlierUse()
    {
        var game = TwoSeatGame(Cross("start"), Cards.Tile("drawn", Rotate, Rotate, Cards.Blank, Cards.Blank));

        var rejected = game.PlaceWithRotates(1, 0, 0, [new RotateUse(0, 0, 1), new RotateUse(4, 4, 1)]);

        AssertRejected(game, rejected, RejectionReason.NoTileToRotate);
        AssertTile(game, 0, 0, ExpectedClockwise(0));
        Assert.That(game.HasTileAt(1, 0), Is.False);
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
    }

    [Test]
    public void OccupiedCell_AndCornerOnly_AreRejected()
    {
        var game = TwoSeatGame(Cards.BlankTile("start"), OneRotate("drawn"));

        var occupied = game.PlaceWithRotates(0, 0, 0, [new RotateUse(0, 0, 1)]);
        AssertRejected(game, occupied, RejectionReason.CellOccupied);

        var corner = game.PlaceWithRotates(1, 1, 0, [new RotateUse(0, 0, 1)]);
        AssertRejected(game, corner, RejectionReason.DoesNotShareFullSide);
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Blank));
    }

    [TestCase(4)]
    [TestCase(-1)]
    public void PlacementQuarterTurnsOutsideZeroThroughThree_AreRejected(int quarterTurns)
    {
        var game = TwoSeatGame(Cards.BlankTile("start"), OneRotate("drawn"));

        CommandResult? result = null;
        Assert.That(() => { result = game.PlaceWithRotates(1, 0, quarterTurns, [new RotateUse(0, 0, 1)]); }, Throws.Nothing);

        Assert.That(result?.IsAccepted, Is.False);
        Assert.That(result?.Rejection?.Reason, Is.EqualTo(RejectionReason.InvalidQuarterTurns));
        Assert.That(result?.Events, Is.Empty);
        Assert.That(result?.Game, Is.SameAs(game));
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
    }

    [Test]
    public void NullUses_Throw_AndLeaveTheGame()
    {
        var game = TwoSeatGame(Cross("start"), OneRotate("drawn"));

        Assert.That(() => game.PlaceWithRotates(1, 0, 0, null!), Throws.ArgumentNullException);
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
    }

    [Test]
    public void EmptyMatchDeck_ThrowsUnresolvedRuling_AndLeavesTheGame()
    {
        var game = TwoSeatGame(Cards.BlankTile("start"));
        var handA = See.Ids(game.Hand(Cards.Seat("a")));
        var handB = See.Ids(game.Hand(Cards.Seat("b")));

        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(
            () => { game.PlaceWithRotates(1, 0, 0, [new RotateUse(0, 0, 1)]); },
            Throws.TypeOf<UnresolvedRulingException>().With.Message.EqualTo(
                "The match deck has no tile to place. Exhausting the match deck is an open ruling, so this command was not applied."));

        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(See.Ids(game.Hand(Cards.Seat("a"))), Is.EqualTo(handA));
        Assert.That(See.Ids(game.Hand(Cards.Seat("b"))), Is.EqualTo(handB));
    }

    // Y increases upward. One clockwise turn moves bottom-left red to top-left.
    private static Cell[] ExpectedClockwise(int quarterTurns) => quarterTurns switch
    {
        0 => [Cards.Red, Cards.Blue, Green, Purple],
        1 => [Cards.Blue, Purple, Cards.Red, Green],
        2 => [Purple, Green, Cards.Blue, Cards.Red],
        3 => [Green, Cards.Red, Purple, Cards.Blue],
        _ => throw new ArgumentOutOfRangeException(nameof(quarterTurns)),
    };

    private static void AssertTile(Game game, int tileX, int tileY, Cell[] cells)
    {
        Assert.That(game.CellAt(tileX * 2, tileY * 2), Is.EqualTo(cells[0]));
        Assert.That(game.CellAt((tileX * 2) + 1, tileY * 2), Is.EqualTo(cells[1]));
        Assert.That(game.CellAt(tileX * 2, (tileY * 2) + 1), Is.EqualTo(cells[2]));
        Assert.That(game.CellAt((tileX * 2) + 1, (tileY * 2) + 1), Is.EqualTo(cells[3]));
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

    private static Tile Cross(string id) => Cards.Tile(id, Cards.Red, Cards.Blue, Green, Purple);

    private static Tile OneRotate(string id) => Cards.Tile(id, Rotate, Cards.Blank, Cards.Blank, Cards.Blank);

    private static Game TwoSeatGame(params Tile[] tiles)
    {
        return RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
            tiles);
    }

    private static Game ReadyToFinishRedRow(int claimsRequiredToWin, params Tile[] following)
    {
        var tiles = new List<Tile>
        {
            Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
            Cards.Tile("finisher", Rotate, Cards.Red, Cards.Blank, Cards.Red),
        };
        tiles.AddRange(following);
        return RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("repl"),
                Cards.Purple("spare"),
            ],
            tiles.ToArray(),
            claimsRequiredToWin);
    }

    private static Game CenterWithAnOpenSouth()
    {
        var game = TwoSeatGame(
            Cross("center"),
            Cards.BlankTile("east"),
            Cards.BlankTile("west"),
            Cross("north"),
            OneRotate("drawn"));
        foreach (var (x, y) in new[] { (1, 0), (-1, 0), (0, 1) })
        {
            game = See.Game(RepresentativeDeck.Play(game, x, y));
        }

        return game;
    }

    private static Game SurroundedCenter()
    {
        var game = TwoSeatGame(
            Cross("center"),
            Cards.BlankTile("east"),
            Cards.BlankTile("west"),
            Cards.BlankTile("north"),
            Cards.BlankTile("south"),
            OneRotate("drawn"));
        foreach (var (x, y) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            game = See.Game(RepresentativeDeck.Play(game, x, y));
        }

        return game;
    }

    private static Game StackedEastSurroundsCenter()
    {
        var game = TwoSeatGame(
            Cross("center"),
            Cards.BlankTile("east"),
            Cards.Tile("lid", Stack, Cards.Blank, Cards.Blank, Cards.Blank),
            Cards.BlankTile("west"),
            Cards.BlankTile("north"),
            Cards.BlankTile("south"),
            OneRotate("drawn"));
        game = See.Game(RepresentativeDeck.Play(game, 1, 0));
        game = See.Game(RepresentativeDeck.Stack(game, 1, 0));
        foreach (var (x, y) in new[] { (-1, 0), (0, 1), (0, -1) })
        {
            game = See.Game(RepresentativeDeck.Play(game, x, y));
        }

        return game;
    }
}
