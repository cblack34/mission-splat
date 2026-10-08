namespace MissionSplat.Rules.Tests;

public class BounceTests
{
    private static readonly Cell Bounce = Cell.Symbol(OrdinaryCatalog.Bounce);

    private static readonly Cell Rotate = Cell.Symbol(OrdinaryCatalog.Rotate);

    private static readonly Cell Stack = Cell.Symbol(OrdinaryCatalog.Stack);

    private static readonly Cell Green = Cell.Color(OrdinaryCatalog.Green);

    private static readonly Cell Purple = Cell.Color(OrdinaryCatalog.Purple);

    [Test]
    public void SingleLayerBounce_RemovesThePosition_AndSendsTheExactTileToTheBottomOfTheDeck()
    {
        var side = Cards.BlankTile("side");
        var next1 = Cards.BlankTile("next1");
        var next2 = Cards.BlankTile("next2");
        var game = TwoSeatGame(Cards.BlankTile("start"), side, OneBounce("drawn"), next1, next2);

        var afterSide = See.Game(RepresentativeDeck.Play(game, 1, 0));
        Assert.That(afterSide.MatchDeckRemaining, Is.EqualTo(3));

        var result = RepresentativeDeck.Bounce(afterSide, 2, 0, [new BounceUse(1, 0)]);
        var next = See.Game(result);

        Assert.That(result.Events, Has.Count.EqualTo(1));
        Assert.That(result.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(next.HasTileAt(1, 0), Is.False, "the bounced position disappears entirely");
        Assert.That(next.HasTileAt(0, 0), Is.True);
        Assert.That(next.HasTileAt(2, 0), Is.True);
        Assert.That(next.TileCount, Is.EqualTo(2));
        Assert.That(next.MatchDeckRemaining, Is.EqualTo(3));
        Assert.That(next.PendingMatchTile!.Id, Is.EqualTo(next1.Id));
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("a")));

        var afterNext1 = See.Game(RepresentativeDeck.Play(next, 0, 1));
        Assert.That(afterNext1.PendingMatchTile!.Id, Is.EqualTo(next2.Id));
        Assert.That(afterNext1.MatchDeckRemaining, Is.EqualTo(2));

        var afterNext2 = See.Game(RepresentativeDeck.Play(afterNext1, 0, 2));
        Assert.That(afterNext2.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(afterNext2.PendingMatchTile, Is.SameAs(side), "the bounced tile is the exact object, last in the deck");
        Assert.That(afterSide.HasTileAt(1, 0), Is.True, "the command before the bounce is unaffected");
    }

    [Test]
    public void TwoBounceCells_BounceTwoTiles_AndAThirdUseIsRejected()
    {
        var drawn = Cards.Tile("drawn", Bounce, Bounce, Cards.Red, Cards.Blue);
        var t1 = Cards.BlankTile("t1");
        var t2 = Cards.BlankTile("t2");
        var game = TwoSeatGame(Cross("start"), t1, t2, drawn);
        var afterT1 = See.Game(RepresentativeDeck.Play(game, 1, 0));
        var afterT2 = See.Game(RepresentativeDeck.Play(afterT1, -1, 0));

        var third = afterT2.PlaceWithBounces(
            2,
            0,
            0,
            [new BounceUse(1, 0), new BounceUse(-1, 0), new BounceUse(1, 0)]);
        AssertRejected(afterT2, third, RejectionReason.TooManyBounceUses);
        Assert.That(afterT2.TileCount, Is.EqualTo(3));
        Assert.That(afterT2.MatchDeckRemaining, Is.EqualTo(1));

        var both = See.Game(RepresentativeDeck.Bounce(
            afterT2,
            2,
            0,
            [new BounceUse(1, 0), new BounceUse(-1, 0)]));
        Assert.That(both.HasTileAt(1, 0), Is.False);
        Assert.That(both.HasTileAt(-1, 0), Is.False);
        Assert.That(both.HasTileAt(2, 0), Is.True);
        Assert.That(both.HasTileAt(0, 0), Is.True);
        Assert.That(both.TileCount, Is.EqualTo(2));
        Assert.That(both.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(both.MatchDeckRemaining, Is.EqualTo(2));
        Assert.That(both.PendingMatchTile, Is.SameAs(t1), "the use order lists t1 before t2, so t1 lands first");

        var afterT1Redrawn = See.Game(RepresentativeDeck.Play(both, 3, 0));
        Assert.That(afterT1Redrawn.PendingMatchTile, Is.SameAs(t2), "t2 trails t1 in the deck, not swapped ahead of it");
    }

    [Test]
    public void BounceSymbolAlreadyOnTheBoard_AddsNoCharge()
    {
        var plain = TwoSeatGame(
            Cards.Tile("start", Bounce, Cards.Red, Cards.Blue, Cards.Blank),
            Cards.BlankTile("drawn"));

        var blocked = plain.PlaceWithBounces(1, 0, 0, [new BounceUse(0, 0)]);

        AssertRejected(plain, blocked, RejectionReason.NoBounceCell);
        Assert.That(plain.CellAt(0, 0), Is.EqualTo(Bounce));
        Assert.That(plain.TileCount, Is.EqualTo(1));
        Assert.That(plain.MatchDeckRemaining, Is.EqualTo(1));
    }

    [Test]
    public void EmptyTarget_IsRejected()
    {
        var game = TwoSeatGame(Cross("start"), OneBounce("drawn"));

        var rejected = game.PlaceWithBounces(1, 0, 0, [new BounceUse(3, 3)]);

        AssertRejected(game, rejected, RejectionReason.NoTileToBounce);
        Assert.That(game.HasTileAt(1, 0), Is.False);
        Assert.That(game.TileCount, Is.EqualTo(1));
    }

    [Test]
    public void RepeatedTargetBeyondItsLayers_IsRejected_RatherThanThrowing()
    {
        var drawn = Cards.Tile("drawn", Bounce, Bounce, Cards.Red, Cards.Blue);
        var game = TwoSeatGame(Cross("start"), Cards.BlankTile("target"), drawn);
        var afterTarget = See.Game(RepresentativeDeck.Play(game, 1, 0));

        var rejected = afterTarget.PlaceWithBounces(
            2,
            0,
            0,
            [new BounceUse(1, 0), new BounceUse(1, 0)]);

        AssertRejected(afterTarget, rejected, RejectionReason.NoTileToBounce);
        Assert.That(afterTarget.HasTileAt(1, 0), Is.True);
        Assert.That(afterTarget.TileCount, Is.EqualTo(2));
    }

    [Test]
    public void StackedPosition_TwoBounces_PeelTwoLayers_RestoringTheCoverTimeCells()
    {
        var mid = Cross("mid");
        var lid = OneStack("lid");
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
            [
                Cross("start"),
                mid,
                OneRotate("turner"),
                lid,
                Cards.Tile("bounce1", Bounce, Bounce, Cards.Blank, Cards.Blank),
                OneBounce("bounce2"),
            ]);

        var afterMid = See.Game(RepresentativeDeck.Play(game, 1, 0));
        AssertTile(afterMid, 1, 0, ExpectedClockwise(0));

        var afterTurn = See.Game(RepresentativeDeck.Rotate(afterMid, 2, 0, [new RotateUse(1, 0, 1)]));
        AssertTile(afterTurn, 1, 0, ExpectedClockwise(1));

        var afterLid = See.Game(RepresentativeDeck.Stack(afterTurn, 1, 0));
        Assert.That(Ids(afterLid.CoveredTileIds(1, 0)), Is.EqualTo(new[] { "mid" }));
        Assert.That(afterLid.TileCount, Is.EqualTo(3));

        // Peel the stack's top: reveals "mid" with the cells it had when covered (rotated), not its
        // original unrotated placement cells.
        var afterFirstBounce = See.Game(RepresentativeDeck.Bounce(afterLid, 3, 0, [new BounceUse(1, 0)]));
        AssertTile(afterFirstBounce, 1, 0, ExpectedClockwise(1));
        Assert.That(afterFirstBounce.HasTileAt(1, 0), Is.True);
        Assert.That(afterFirstBounce.CoveredTileIds(1, 0), Is.Empty);
        Assert.That(afterFirstBounce.TileCount, Is.EqualTo(4), "a stacked bounce does not change the tile count");

        // A second bounce on the now single-layer position removes it entirely.
        var afterSecondBounce = See.Game(RepresentativeDeck.Bounce(afterFirstBounce, 4, 0, [new BounceUse(1, 0)]));
        Assert.That(afterSecondBounce.HasTileAt(1, 0), Is.False);
        Assert.That(afterSecondBounce.TileCount, Is.EqualTo(4));
        Assert.That(afterSecondBounce.MatchDeckRemaining, Is.EqualTo(2));
        Assert.That(afterSecondBounce.PendingMatchTile, Is.SameAs(lid), "lid was bounced first");

        // The same two-layer peel, but as both uses of one command on the same target: "bounce1" has two
        // bounce cells, so one PlaceWithBounces call reveals "mid" and then removes it in the same turn.
        var bothInOneCommand = See.Game(RepresentativeDeck.Bounce(
            afterLid,
            3,
            0,
            [new BounceUse(1, 0), new BounceUse(1, 0)]));
        Assert.That(bothInOneCommand.HasTileAt(1, 0), Is.False);
        Assert.That(bothInOneCommand.TileCount, Is.EqualTo(3), "start, turner, and bounce1 remain");
        Assert.That(bothInOneCommand.MatchDeckRemaining, Is.EqualTo(3));
        Assert.That(bothInOneCommand.PendingMatchTile!.Id.Value, Is.EqualTo("bounce2"), "lid and mid trail it");

        var afterBounce2 = See.Game(RepresentativeDeck.Play(bothInOneCommand, 4, 0));
        Assert.That(afterBounce2.PendingMatchTile, Is.SameAs(lid), "lid was bounced before mid in this one command");

        var afterLidRedrawn = See.Game(RepresentativeDeck.Play(afterBounce2, 5, 0));
        Assert.That(afterLidRedrawn.PendingMatchTile, Is.SameAs(mid), "mid trails lid, not swapped ahead of it");
    }

    [Test]
    public void SelfBounce_IsRejected_AndLeavesTheGameUnchanged()
    {
        var game = TwoSeatGame(Cross("start"), OneBounce("drawn"));

        var rejected = game.PlaceWithBounces(1, 0, 0, [new BounceUse(1, 0)]);

        AssertRejected(game, rejected, RejectionReason.CannotBounceJustPlacedTile);
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.HasTileAt(1, 0), Is.False);
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
    }

    [Test]
    public void Place_Stack_AndPlaceWithRotates_DeclineBounce()
    {
        var placeGame = TwoSeatGame(Cross("start"), OneBounce("drawn"));
        var placed = See.Game(RepresentativeDeck.Play(placeGame, 1, 0));
        Assert.That(placed.HasTileAt(0, 0), Is.True);
        Assert.That(placed.TileCount, Is.EqualTo(2));
        Assert.That(placed.CellAt(2, 0), Is.EqualTo(Bounce));

        var stackGame = TwoSeatGame(
            Cards.BlankTile("start"),
            Cards.BlankTile("aside"),
            Cards.Tile("drawn", Bounce, Stack, Cards.Blank, Cards.Blank));
        var afterAside = See.Game(RepresentativeDeck.Play(stackGame, 1, 0));
        var stacked = See.Game(RepresentativeDeck.Stack(afterAside, 0, 0));
        Assert.That(stacked.HasTileAt(1, 0), Is.True, "stack does not bounce the aside tile");
        Assert.That(stacked.TileCount, Is.EqualTo(2));

        var rotateGame = TwoSeatGame(Cross("start"), Cards.Tile("drawn", Bounce, Rotate, Cards.Blank, Cards.Blank));
        var rotated = See.Game(RepresentativeDeck.Rotate(rotateGame, 1, 0, [new RotateUse(0, 0, 1)]));
        AssertTile(rotated, 0, 0, ExpectedClockwise(1));
        Assert.That(rotated.TileCount, Is.EqualTo(2), "rotate does not also bounce");
        Assert.That(rotated.CellAt(2, 0), Is.EqualTo(Bounce));
    }

    [Test]
    public void NoUse_IsRejected_EvenWhenTheDrawnTileHasABounceCell()
    {
        var game = TwoSeatGame(Cross("start"), OneBounce("drawn"));

        var rejected = game.PlaceWithBounces(1, 0, 0, []);

        AssertRejected(game, rejected, RejectionReason.NoBounceUse);
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
    }

    [Test]
    public void NullUses_Throw_AndLeaveTheGame()
    {
        var game = TwoSeatGame(Cross("start"), OneBounce("drawn"));

        Assert.That(() => game.PlaceWithBounces(1, 0, 0, null!), Throws.ArgumentNullException);
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
    }

    // Mirrors RotateTests.OccupiedCell_AndCornerOnly_AreRejected: RejectBlockedSide is shared logic, but this
    // pins it specifically through the PlaceWithBounces call site. The single bounce use is never reached:
    // RejectBounceUses (and then RejectBlockedSide) both run before RejectBounceTargets is consulted.
    [Test]
    public void OccupiedCell_AndCornerOnly_AreRejected()
    {
        var game = TwoSeatGame(Cards.BlankTile("start"), OneBounce("drawn"));

        var occupied = game.PlaceWithBounces(0, 0, 0, [new BounceUse(0, 0)]);
        AssertRejected(game, occupied, RejectionReason.CellOccupied);

        var corner = game.PlaceWithBounces(1, 1, 0, [new BounceUse(0, 0)]);
        AssertRejected(game, corner, RejectionReason.DoesNotShareFullSide);
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Blank));
    }

    // Mirrors RotateTests.EmptyMatchDeck_ThrowsUnresolvedRuling_AndLeavesTheGame. RejectBeforeConsumingMatchTile
    // is shared, unchanged logic that runs before any bounce-specific code, reached the same way through
    // PlaceWithBounces as through Place, PlaceWithRotates, and Stack.
    [Test]
    public void EmptyMatchDeck_ThrowsUnresolvedRuling_AndLeavesTheGame()
    {
        var game = TwoSeatGame(Cards.BlankTile("start"));
        var handA = See.Ids(game.Hand(Cards.Seat("a")));
        var handB = See.Ids(game.Hand(Cards.Seat("b")));

        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(
            () => { game.PlaceWithBounces(1, 0, 0, [new BounceUse(0, 0)]); },
            Throws.TypeOf<UnresolvedRulingException>().With.Message.EqualTo(
                "The match deck has no tile to place. Exhausting the match deck is an open ruling, so this command was not applied."));

        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(See.Ids(game.Hand(Cards.Seat("a"))), Is.EqualTo(handA));
        Assert.That(See.Ids(game.Hand(Cards.Seat("b"))), Is.EqualTo(handB));
    }

    // The square's four corners are (1,1), (2,1), (1,2), and (2,2), one cell from each of four tiles (see
    // SquareClaimTests). Bouncing "start" away in the same command that would otherwise complete the square
    // denies the claim: ResolvePlacement reads the post-bounce board, and "start" no longer holds corner (1,1).
    // This is the "bounce that removes a would-be pattern" gap flagged against claim coverage for this command.
    [Test]
    public void BounceRemovesAWouldBePattern_IsNotClaimed()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("spare"),
            ],
            [
                Cards.Tile("start", Cards.Blank, Cards.Blank, Cards.Blank, Cards.Red),
                Cards.Tile("a-side", Cards.Blank, Cards.Blank, Cards.Red, Cards.Blank),
                Cards.Tile("b-side", Cards.Blank, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("finish", Cards.Red, Bounce, Cards.Blank, Cards.Blank),
            ]);

        var afterA = See.Game(RepresentativeDeck.Play(game, 1, 0));
        var afterB = See.Game(RepresentativeDeck.Play(afterA, 0, 1));

        var result = RepresentativeDeck.Bounce(afterB, 1, 1, [new BounceUse(0, 0)]);
        var next = See.Game(result);

        Assert.That(next.HasTileAt(0, 0), Is.False, "the removed corner leaves the board");
        Assert.That(next.CellAt(2, 2), Is.EqualTo(Cards.Red), "the placed tile still wrote its own corner");
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty, "the square never completed: bounce removed a corner");
        Assert.That(result.Events, Has.Count.EqualTo(1));
        Assert.That(result.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(next.TileCount, Is.EqualTo(3));
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
    }

    // "start" is a solid green square: once dealt, all four of its cells already match the square pattern, but
    // Start never evaluates claims. Covering it with a stack hides the match; bouncing that stack away in a
    // later, unrelated command (one that never writes any of the square's own four cells) still does not
    // claim it, because WasCompletedBy requires the acting command's written cells to touch the pattern. This
    // is the "remote bounce/reveal that forms a pattern missing the written cells" gap flagged against claim
    // coverage for this command.
    [Test]
    public void RemoteBounceReveal_FormsAPatternMissingTheWrittenCells_IsNotClaimed()
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
                Cards.Solid("start", Green),
                Cards.BlankTile("pad"),
                Cards.Tile("cover", Stack, Cards.Blank, Cards.Blank, Cards.Blank),
                Cards.Tile("elsewhere", Bounce, Cards.Blank, Cards.Blank, Cards.Blank),
            ]);

        var afterPad = See.Game(RepresentativeDeck.Play(game, 1, 0));
        var stacked = See.Game(RepresentativeDeck.Stack(afterPad, 0, 0));
        Assert.That(stacked.CellAt(0, 0), Is.EqualTo(Stack));
        Assert.That(stacked.Claims(Cards.Seat("a")), Is.Empty);

        var result = RepresentativeDeck.Bounce(stacked, 2, 0, [new BounceUse(0, 0)]);
        var next = See.Game(result);

        Assert.That(next.CellAt(0, 0), Is.EqualTo(Green));
        Assert.That(next.CellAt(1, 0), Is.EqualTo(Green));
        Assert.That(next.CellAt(0, 1), Is.EqualTo(Green));
        Assert.That(next.CellAt(1, 1), Is.EqualTo(Green));
        Assert.That(next.CoveredTileIds(0, 0), Is.Empty);
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty, "the reveal writes none of the square's own cells");
        Assert.That(result.Events, Has.Count.EqualTo(1));
        Assert.That(result.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
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

    private static Tile OneBounce(string id) => Cards.Tile(id, Bounce, Cards.Blank, Cards.Blank, Cards.Blank);

    private static Tile OneRotate(string id) => Cards.Tile(id, Rotate, Cards.Blank, Cards.Blank, Cards.Blank);

    private static Tile OneStack(string id) => Cards.Tile(id, Stack, Cards.Blank, Cards.Blank, Cards.Blank);

    private static Game TwoSeatGame(params Tile[] tiles)
    {
        return RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
            tiles);
    }
}
