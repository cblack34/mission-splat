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
    public void RotateOnATileWithAFreeSide_TurnsOnlyThatTilesCells_EmitsOneEvent_AndLeavesTheDrawnTilePending(
        int quarterTurns)
    {
        var game = RepresentativeDeck.TwoSeats(Cards.Cross("start"), Cards.BlankTile("pad"), OneRotate("drawn"));
        var padded = See.Game(RepresentativeDeck.Play(game, 1, 0));

        var result = RepresentativeDeck.Rotate(padded, 0, 0, quarterTurns);
        var next = See.Game(result);

        Expect.Tile(next, 0, 0, Oriented.Cross(quarterTurns));
        Expect.Tile(next, 1, 0, [Cards.Blank, Cards.Blank, Cards.Blank, Cards.Blank]);
        Assert.That(next.CellAt(4, 4), Is.Null);
        Assert.That(next.TileCount, Is.EqualTo(2));
        Assert.That(next.CoveredTileIds(0, 0), Is.Empty);
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(next.PendingMatchTile!.Id.Value, Is.EqualTo("drawn"));
        Assert.That(next.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(next.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(result.Events, Is.EqualTo(new[] { new TileRotated(Cards.Seat("b"), 0, 0, quarterTurns) }));
        Expect.Tile(padded, 0, 0, Oriented.Cross(0));
    }

    [Test]
    public void RotateBeforeThePlacement_AndThePlacementQuarterTurns_AreIndependent()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.Cross("start"), Cards.Tile("spun", Rotate, Cards.Red, Cards.Blue, Green));

        var rotated = See.Game(RepresentativeDeck.Rotate(game, 0, 0));
        var placed = RepresentativeDeck.Play(rotated, 1, 0, 2);
        var next = See.Game(placed);

        Expect.Tile(next, 0, 0, Oriented.Cross(1));
        Expect.Tile(next, 1, 0, [Green, Cards.Blue, Cards.Red, Rotate]);
        Assert.That(((TilePlaced)placed.Events[0]).QuarterTurnsClockwise, Is.EqualTo(2));
        Assert.That(next.TileCount, Is.EqualTo(2));
    }

    [Test]
    public void FourOrthogonalNeighbors_RejectTheRotate_AndLeaveTheMatchUnchanged()
    {
        var game = SurroundedCenter();
        var handA = See.Ids(game.Hand(Cards.Seat("a")));
        var missionsLeft = game.MissionDeckRemaining;

        var rejected = RepresentativeDeck.Try(game, new UseRotate(0, 0, 1));

        Expect.Rejected(game, rejected, RejectionReason.TileSurrounded);
        Expect.Tile(game, 0, 0, Oriented.Cross(0));
        Assert.That(game.TileCount, Is.EqualTo(5));
        Assert.That(game.PendingMatchTile!.Id.Value, Is.EqualTo("drawn"));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.MissionDeckRemaining, Is.EqualTo(missionsLeft));
        Assert.That(See.Ids(game.Hand(Cards.Seat("a"))), Is.EqualTo(handA));

        var chargeIntact = RepresentativeDeck.Rotate(game, 1, 0);
        Assert.That(chargeIntact.Events, Has.Count.EqualTo(1), "the rejected use did not spend the rotate cell");
    }

    [Test]
    public void TheDrawnTile_IsNotOnTheBoard_SoItNeverSurroundsATarget()
    {
        var game = CenterWithAnOpenSouth();

        var result = RepresentativeDeck.Rotate(game, 0, 0);
        var next = See.Game(result);

        Expect.Tile(next, 0, 0, Oriented.Cross(1));
        Assert.That(next.TileCount, Is.EqualTo(4));
        Assert.That(next.HasTileAt(0, -1), Is.False);
        Assert.That(next.PendingMatchTile!.Id.Value, Is.EqualTo("drawn"));
    }

    [Test]
    public void DiagonalNeighbors_DoNotCountAsSurrounded()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.Cross("center"),
            Cards.BlankTile("east"),
            Cards.BlankTile("west"),
            Cards.BlankTile("north"),
            Cards.BlankTile("southeast"),
            Cards.BlankTile("southwest"),
            OneRotate("drawn"));
        foreach (var (x, y) in new[] { (1, 0), (-1, 0), (0, 1), (1, -1), (-1, -1) })
        {
            game = See.Game(RepresentativeDeck.Play(game, x, y));
        }

        var result = RepresentativeDeck.Rotate(game, 0, 0);
        var next = See.Game(result);

        Expect.Tile(next, 0, 0, Oriented.Cross(1));
        Assert.That(next.TileCount, Is.EqualTo(6));
        Assert.That(next.HasTileAt(0, -1), Is.False);
    }

    [Test]
    public void CoveredStack_TurnsOnlyItsVisibleCells_AndAStackedNeighborStillSurrounds()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.Cross("buried"),
            Cards.Tile("top", Cards.Red, Cards.Blue, Green, Stack),
            OneRotate("drawn"));
        var covered = See.Game(RepresentativeDeck.Play(game, 0, 0));

        var next = See.Game(RepresentativeDeck.Rotate(covered, 0, 0));

        Expect.Tile(next, 0, 0, [Cards.Blue, Stack, Cards.Red, Green]);
        Assert.That(See.Ids(next.CoveredTileIds(0, 0)), Is.EqualTo(new[] { "buried" }));
        Assert.That(next.TileCount, Is.EqualTo(1));

        var blocked = StackedEastSurroundsCenter();
        var rejected = RepresentativeDeck.Try(blocked, new UseRotate(0, 0, 1));

        Expect.Rejected(blocked, rejected, RejectionReason.TileSurrounded);
        Assert.That(See.Ids(blocked.CoveredTileIds(1, 0)), Is.EqualTo(new[] { "east" }));
        Assert.That(blocked.CellAt(2, 0), Is.EqualTo(Stack));
        Assert.That(blocked.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(blocked.TileCount, Is.EqualTo(5));
    }

    [Test]
    public void TwoRotateCells_AllowTwoUsesOnTwoTilesOrOneTile_AndAThirdUseIsRejected()
    {
        var drawn = Cards.Tile("drawn", Rotate, Rotate, Cards.Red, Cards.Blue);
        var game = RepresentativeDeck.TwoSeats(Cards.Cross("start"), Cards.BlankTile("pad"), drawn);
        var afterPad = See.Game(RepresentativeDeck.Play(game, 1, 0));

        var afterFirst = See.Game(RepresentativeDeck.Rotate(afterPad, 0, 0));
        var afterSecond = See.Game(RepresentativeDeck.Rotate(afterFirst, 1, 0, 2));
        var third = RepresentativeDeck.Try(afterSecond, new UseRotate(0, 0, 1));

        Expect.Rejected(afterSecond, third, RejectionReason.NoUseRemaining);
        Expect.Tile(afterSecond, 0, 0, Oriented.Cross(1));
        Assert.That(afterSecond.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(afterSecond.PendingMatchTile!.Id.Value, Is.EqualTo("drawn"));

        var sameTileTwice = See.Game(RepresentativeDeck.Rotate(afterFirst, 0, 0));
        Expect.Tile(sameTileTwice, 0, 0, Oriented.Cross(2));
        Assert.That(
            RepresentativeDeck.Try(sameTileTwice, new UseRotate(0, 0, 1)).Rejection?.Reason,
            Is.EqualTo(RejectionReason.NoUseRemaining));
    }

    [Test]
    public void ARejectedSecondUse_LeavesTheFirstApplied_AndTheRemainingChargeIntact()
    {
        var drawn = Cards.Tile("drawn", Rotate, Rotate, Cards.Blank, Cards.Blank);
        var game = RepresentativeDeck.TwoSeats(Cards.Cross("start"), drawn);
        var afterFirst = See.Game(RepresentativeDeck.Rotate(game, 0, 0));

        var rejected = RepresentativeDeck.Try(afterFirst, new UseRotate(3, 3, 1));

        Expect.Rejected(afterFirst, rejected, RejectionReason.NoTileToRotate);
        Expect.Tile(afterFirst, 0, 0, Oriented.Cross(1));
        Assert.That(afterFirst.PendingMatchTile!.Id.Value, Is.EqualTo("drawn"));
        Assert.That(afterFirst.CurrentSeat, Is.EqualTo(Cards.Seat("a")));

        var afterSecond = See.Game(RepresentativeDeck.Rotate(afterFirst, 0, 0));
        Expect.Tile(afterSecond, 0, 0, Oriented.Cross(2));
    }

    [Test]
    public void ARejectedPlacement_AfterAnAcceptedUse_LeavesTheUseApplied_TheDrawnTilePending_AndTheSameSeat()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.Cross("start"), OneRotate("drawn"));
        var afterUse = See.Game(RepresentativeDeck.Rotate(game, 0, 0));

        var occupied = RepresentativeDeck.Try(afterUse, new Place(0, 0, 0));
        var corner = RepresentativeDeck.Try(afterUse, new Place(1, 1, 0));

        Expect.Rejected(afterUse, occupied, RejectionReason.CellOccupied);
        Expect.Rejected(afterUse, corner, RejectionReason.DoesNotShareFullSide);
        Expect.Tile(afterUse, 0, 0, Oriented.Cross(1));
        Assert.That(afterUse.PendingMatchTile!.Id.Value, Is.EqualTo("drawn"));
        Assert.That(afterUse.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(afterUse.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(afterUse.TileCount, Is.EqualTo(1));
        Assert.That(
            RepresentativeDeck.Try(afterUse, new UseRotate(0, 0, 1)).Rejection?.Reason,
            Is.EqualTo(RejectionReason.NoUseRemaining),
            "the accepted use stays spent");

        var placed = See.Game(RepresentativeDeck.Play(afterUse, 1, 0));
        Expect.Tile(placed, 0, 0, Oriented.Cross(1));
        Assert.That(placed.TileCount, Is.EqualTo(2));
    }

    [Test]
    public void AUseAfterThePlacement_IsRejected_AndEachNewDrawnTileCarriesItsOwnCharge()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.Cross("start"), OneRotate("first"), OneRotate("second"), Cards.BlankTile("third"));
        var used = See.Game(RepresentativeDeck.Rotate(game, 0, 0));
        var afterPlacement = RepresentativeDeck.Play(used, 1, 0);
        var next = See.Game(afterPlacement);

        var byThePlacingSeat = game.Apply(Cards.Seat("a"), new UseRotate(0, 0, 1));
        var afterwards = next.Apply(Cards.Seat("a"), new UseRotate(0, 0, 1));

        Assert.That(byThePlacingSeat.IsAccepted, Is.True, "a rotate before the placement is the seat's own turn");
        Expect.Rejected(next, afterwards, RejectionReason.NotYourTurn);
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(next.PendingMatchTile!.Id.Value, Is.EqualTo("second"));
        Assert.That(RepresentativeDeck.Rotate(next, 1, 0).Events, Has.Count.EqualTo(1), "a placement resets the charge");

        var third = See.Game(RepresentativeDeck.Play(next, 0, 1));
        var noCharge = RepresentativeDeck.Try(third, new UseRotate(0, 0, 1));
        Expect.Rejected(third, noCharge, RejectionReason.NoUseRemaining);
    }

    [Test]
    public void RotateSymbolAlreadyOnTheBoard_GrantsNothing()
    {
        var plain = RepresentativeDeck.TwoSeats(
            Cards.Tile("start", Rotate, Cards.Red, Cards.Blue, Cards.Blank),
            Cards.BlankTile("drawn"));
        var blocked = RepresentativeDeck.Try(plain, new UseRotate(0, 0, 1));

        Expect.Rejected(plain, blocked, RejectionReason.NoUseRemaining);
        Assert.That(plain.CellAt(0, 0), Is.EqualTo(Rotate));
        Assert.That(plain.TileCount, Is.EqualTo(1));

        var charged = RepresentativeDeck.TwoSeats(
            Cards.Tile("start", Rotate, Cards.Red, Cards.Blue, Cards.Blank),
            OneRotate("drawn"));
        var once = See.Game(RepresentativeDeck.Rotate(charged, 0, 0));
        Expect.Tile(once, 0, 0, [Cards.Red, Cards.Blank, Rotate, Cards.Blue]);
        Expect.Rejected(
            once,
            RepresentativeDeck.Try(once, new UseRotate(0, 0, 1)),
            RejectionReason.NoUseRemaining);
    }

    [Test]
    public void Place_DoesNotRotate_AndDoesNotClaimTheUnturnedRow()
    {
        var game = ReadyToFinishRedRow(OrdinaryCatalog.ClaimsRequiredToWin);

        var placed = RepresentativeDeck.Play(game, 1, 0);
        var next = See.Game(placed);

        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(1, 0), Is.EqualTo(Cards.Blank));
        Assert.That(next.CellAt(2, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(3, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row", "a2" }));
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(placed.Events, Has.Count.EqualTo(1));
        Assert.That(placed.Events[0], Is.TypeOf<TilePlaced>());
    }

    [Test]
    public void ARotateBeforeThePlacement_ThatLinesUpTheRow_ClaimsOnlyThroughThePlacedCells()
    {
        var game = ReadyToFinishRedRow(OrdinaryCatalog.ClaimsRequiredToWin);

        var rotated = RepresentativeDeck.Rotate(game, 0, 0, 3);
        Assert.That(rotated.Events, Is.EqualTo(new[] { new TileRotated(Cards.Seat("a"), 0, 0, 3) }));
        var afterRotate = See.Game(rotated);
        Assert.That(afterRotate.Claims(Cards.Seat("a")), Is.Empty, "a use never claims by itself");
        Assert.That(afterRotate.CurrentSeat, Is.EqualTo(Cards.Seat("a")));

        var placed = RepresentativeDeck.Play(afterRotate, 1, 0);
        var next = See.Game(placed);

        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(1, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(2, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(3, 0), Is.EqualTo(Cards.Red));
        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "a2", "repl" }));
        Assert.That(next.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(next.HasEnded, Is.False);
        Assert.That(placed.Events, Has.Count.EqualTo(2));
        Assert.That(placed.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(((MissionClaimed)placed.Events[1]).Mission.Value, Is.EqualTo("red-row"));
        Assert.That(placed.Events.OfType<GameWon>(), Is.Empty);
        Assert.That(game.CellAt(1, 0), Is.EqualTo(Cards.Blank));
    }

    [Test]
    public void ARotateBeforeAWinningPlacement_EmitsWonAfterTheClaim_AndLaterActionsAreRejected()
    {
        var game = ReadyToFinishRedRow(1, Cards.BlankTile("later"));

        var afterRotate = See.Game(RepresentativeDeck.Rotate(game, 0, 0, 3));
        var result = RepresentativeDeck.Play(afterRotate, 1, 0);
        var won = See.Game(result);

        Assert.That(result.Events, Has.Count.EqualTo(3));
        Assert.That(result.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(((MissionClaimed)result.Events[1]).Mission.Value, Is.EqualTo("red-row"));
        var gameWon = (GameWon)result.Events[2];
        Assert.That(gameWon.Seat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(gameWon.ClaimCount, Is.EqualTo(1));
        Assert.That(won.HasEnded, Is.True);
        Assert.That(won.PendingMatchTile, Is.Null);

        Expect.Rejected(
            won,
            RepresentativeDeck.Try(won, new UseRotate(0, 0, 1)),
            RejectionReason.GameOver);
        Expect.Rejected(won, RepresentativeDeck.Try(won, new Place(2, 0, 0)), RejectionReason.GameOver);
        Assert.That(won.TileCount, Is.EqualTo(2));
    }

    [Test]
    public void ARotateThatFormsAPattern_ThePlacedCellsDoNotComplete_DoesNotClaim()
    {
        // East completes the red row only after it turns. The placed tile goes on the other side of the start.
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

        var afterRotate = See.Game(RepresentativeDeck.Rotate(ready, 1, 0));
        var placed = RepresentativeDeck.Play(afterRotate, -1, 0);
        var next = See.Game(placed);

        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(1, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(2, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(3, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.CellAt(-2, 0), Is.EqualTo(Rotate));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row", "a2" }));
        Assert.That(placed.Events, Has.Count.EqualTo(1));
        Assert.That(placed.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(next.TileCount, Is.EqualTo(4));
    }

    [TestCase(0)]
    [TestCase(4)]
    [TestCase(-1)]
    public void RotateUseOutsideOneThroughThree_IsRejected(int quarterTurns)
    {
        var game = RepresentativeDeck.TwoSeats(Cards.Cross("start"), OneRotate("drawn"));

        var rejected = RepresentativeDeck.Try(game, new UseRotate(0, 0, quarterTurns));

        Expect.Rejected(game, rejected, RejectionReason.InvalidRotateQuarterTurns);
        Assert.That(rejected.Rejection?.Message, Is.EqualTo("A rotate use is 1, 2, or 3 quarter-turns."));
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Red));
    }

    [Test]
    public void EmptyTarget_IsRejected()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.Cross("start"), OneRotate("drawn"));

        var rejected = RepresentativeDeck.Try(game, new UseRotate(3, 3, 1));

        Expect.Rejected(game, rejected, RejectionReason.NoTileToRotate);
        Assert.That(game.HasTileAt(3, 3), Is.False);
        Assert.That(game.TileCount, Is.EqualTo(1));
    }

    [Test]
    public void EmptyMatchDeck_ThrowsUnresolvedRuling_AndLeavesTheGame()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"));
        var handA = See.Ids(game.Hand(Cards.Seat("a")));
        var handB = See.Ids(game.Hand(Cards.Seat("b")));

        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(
            () => { RepresentativeDeck.Try(game, new UseRotate(0, 0, 1)); },
            Throws.TypeOf<UnresolvedRulingException>().With.Message.EqualTo(
                "The match deck has no tile to place. Exhausting the match deck is an open ruling, so this command was not applied."));

        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(See.Ids(game.Hand(Cards.Seat("a"))), Is.EqualTo(handA));
        Assert.That(See.Ids(game.Hand(Cards.Seat("b"))), Is.EqualTo(handB));
    }

    private static Tile OneRotate(string id) => Cards.Tile(id, Rotate, Cards.Blank, Cards.Blank, Cards.Blank);

    // Turning the start tile three quarter-turns brings its left column of red into the bottom row, so the
    // finisher's own red cells then complete the row of four.
    private static Game ReadyToFinishRedRow(int claimsRequiredToWin, params Tile[] following)
    {
        var tiles = new List<Tile>
        {
            Cards.Tile("start", Cards.Red, Cards.Blank, Cards.Red, Cards.Blank),
            Cards.Tile("finisher", Cards.Red, Cards.Red, Rotate, Cards.Blank),
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
        var game = RepresentativeDeck.TwoSeats(
            Cards.Cross("center"),
            Cards.BlankTile("east"),
            Cards.BlankTile("west"),
            Cards.Cross("north"),
            OneRotate("drawn"));
        foreach (var (x, y) in new[] { (1, 0), (-1, 0), (0, 1) })
        {
            game = See.Game(RepresentativeDeck.Play(game, x, y));
        }

        return game;
    }

    private static Game SurroundedCenter()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.Cross("center"),
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
        var game = RepresentativeDeck.TwoSeats(
            Cards.Cross("center"),
            Cards.BlankTile("east"),
            Cards.Tile("lid", Stack, Cards.Blank, Cards.Blank, Cards.Blank),
            Cards.BlankTile("west"),
            Cards.BlankTile("north"),
            Cards.BlankTile("south"),
            OneRotate("drawn"));
        game = See.Game(RepresentativeDeck.Play(game, 1, 0));
        game = See.Game(RepresentativeDeck.Play(game, 1, 0));
        foreach (var (x, y) in new[] { (-1, 0), (0, 1), (0, -1) })
        {
            game = See.Game(RepresentativeDeck.Play(game, x, y));
        }

        return game;
    }
}
