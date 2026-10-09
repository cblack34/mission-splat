namespace MissionSplat.Rules.Tests;

public class BounceTests
{
    private static readonly Cell Bounce = Cell.Symbol(OrdinaryCatalog.Bounce);

    private static readonly Cell Rotate = Cell.Symbol(OrdinaryCatalog.Rotate);

    private static readonly Cell Stack = Cell.Symbol(OrdinaryCatalog.Stack);

    private static readonly Cell Green = Cell.Color(OrdinaryCatalog.Green);

    private static readonly Cell Purple = Cell.Color(OrdinaryCatalog.Purple);

    [Test]
    public void BounceBeforeThePlacement_RemovesThePosition_SendsTheTileToTheBottomOfTheDeck_AndLeavesTheDrawnTilePending()
    {
        var side = Cards.BlankTile("side");
        var next1 = Cards.BlankTile("next1");
        var next2 = Cards.BlankTile("next2");
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"), side, OneBounce("drawn"), next1, next2);
        var afterSide = See.Game(RepresentativeDeck.Play(game, 1, 0));
        Assert.That(afterSide.MatchDeckRemaining, Is.EqualTo(3));

        var result = RepresentativeDeck.Bounce(afterSide, 1, 0);
        var bounced = See.Game(result);

        Assert.That(result.Events, Is.EqualTo(new[] { new TileBounced(Cards.Seat("b"), 1, 0, new TileId("side"), null) }));
        Assert.That(bounced.HasTileAt(1, 0), Is.False, "the bounced position disappears entirely");
        Assert.That(bounced.CellAt(2, 0), Is.Null);
        Assert.That(bounced.HasTileAt(0, 0), Is.True);
        Assert.That(bounced.TileCount, Is.EqualTo(1));
        Assert.That(bounced.MatchDeckRemaining, Is.EqualTo(4));
        Assert.That(bounced.PendingMatchTile!.Id.Value, Is.EqualTo("drawn"));
        Assert.That(bounced.CurrentSeat, Is.EqualTo(Cards.Seat("b")));

        var afterDrawn = See.Game(RepresentativeDeck.Play(bounced, 1, 0));
        Assert.That(afterDrawn.PendingMatchTile, Is.SameAs(next1));
        var afterNext1 = See.Game(RepresentativeDeck.Play(afterDrawn, 0, 1));
        Assert.That(afterNext1.PendingMatchTile, Is.SameAs(next2));
        var afterNext2 = See.Game(RepresentativeDeck.Play(afterNext1, 0, -1));
        Assert.That(afterNext2.PendingMatchTile, Is.SameAs(side), "the bounced tile is last in the deck");
        Assert.That(afterSide.HasTileAt(1, 0), Is.True, "the match before the bounce is unaffected");
    }

    [Test]
    public void TwoBounceCells_BounceTwoTiles_AndAThirdUseIsRejected()
    {
        var drawn = Cards.Tile("drawn", Bounce, Bounce, Cards.Red, Cards.Blue);
        var t1 = Cards.BlankTile("t1");
        var t2 = Cards.BlankTile("t2");
        var game = RepresentativeDeck.TwoSeats(Cards.Cross("start"), t1, t2, drawn);
        var afterT1 = See.Game(RepresentativeDeck.Play(game, 1, 0));
        var afterT2 = See.Game(RepresentativeDeck.Play(afterT1, -1, 0));

        var afterFirst = See.Game(RepresentativeDeck.Bounce(afterT2, 1, 0));
        var afterSecond = See.Game(RepresentativeDeck.Bounce(afterFirst, -1, 0));
        var third = RepresentativeDeck.Try(afterSecond, new UseBounce(0, 0));

        Expect.Rejected(afterSecond, third, RejectionReason.NoUseRemaining);
        Assert.That(afterSecond.HasTileAt(1, 0), Is.False);
        Assert.That(afterSecond.HasTileAt(-1, 0), Is.False);
        Assert.That(afterSecond.HasTileAt(0, 0), Is.True);
        Assert.That(afterSecond.TileCount, Is.EqualTo(1));
        Assert.That(afterSecond.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(afterSecond.PendingMatchTile!.Id.Value, Is.EqualTo("drawn"));
        Assert.That(afterSecond.MatchDeckRemaining, Is.EqualTo(3));

        var afterDrawn = See.Game(RepresentativeDeck.Play(afterSecond, 1, 0));
        Assert.That(afterDrawn.PendingMatchTile, Is.SameAs(t1), "the use order lists t1 before t2, so t1 lands first");
        var afterT1Redrawn = See.Game(RepresentativeDeck.Play(afterDrawn, 2, 0));
        Assert.That(afterT1Redrawn.PendingMatchTile, Is.SameAs(t2), "t2 trails t1 in the deck, not swapped ahead of it");
    }

    [Test]
    public void BounceSymbolAlreadyOnTheBoard_GrantsNothing()
    {
        var plain = RepresentativeDeck.TwoSeats(
            Cards.Tile("start", Bounce, Cards.Red, Cards.Blue, Cards.Blank),
            Cards.BlankTile("drawn"));

        var blocked = RepresentativeDeck.Try(plain, new UseBounce(0, 0));

        Expect.Rejected(plain, blocked, RejectionReason.NoUseRemaining);
        Assert.That(plain.CellAt(0, 0), Is.EqualTo(Bounce));
        Assert.That(plain.TileCount, Is.EqualTo(1));
        Assert.That(plain.MatchDeckRemaining, Is.EqualTo(1));
    }

    [Test]
    public void EmptyTarget_IsRejected_AndTheDrawnTileIsNeverATarget()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.Cross("start"), OneBounce("drawn"));

        foreach (var (x, y) in new[] { (3, 3), (1, 0) })
        {
            var rejected = RepresentativeDeck.Try(game, new UseBounce(x, y));

            Expect.Rejected(game, rejected, RejectionReason.NoTileToBounce);
        }

        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.PendingMatchTile!.Id.Value, Is.EqualTo("drawn"));
    }

    [Test]
    public void ASecondBounceOfAnAlreadyEmptiedPosition_IsRejected_AndTheFirstStaysApplied()
    {
        var drawn = Cards.Tile("drawn", Bounce, Bounce, Cards.Red, Cards.Blue);
        var game = RepresentativeDeck.TwoSeats(Cards.Cross("start"), Cards.BlankTile("target"), drawn);
        var afterTarget = See.Game(RepresentativeDeck.Play(game, 1, 0));
        var afterFirst = See.Game(RepresentativeDeck.Bounce(afterTarget, 1, 0));

        var rejected = RepresentativeDeck.Try(afterFirst, new UseBounce(1, 0));

        Expect.Rejected(afterFirst, rejected, RejectionReason.NoTileToBounce);
        Assert.That(afterFirst.HasTileAt(1, 0), Is.False);
        Assert.That(afterFirst.TileCount, Is.EqualTo(1));
        Assert.That(RepresentativeDeck.Bounce(afterFirst, 0, 0).Events, Has.Count.EqualTo(1), "the second cell is still unspent");
    }

    [Test]
    public void StackedPosition_TwoBounces_PeelTwoLayers_RestoringTheCoverTimeCells()
    {
        var mid = Cards.Cross("mid");
        var lid = OneStack("lid");
        var bounce2 = OneBounce("bounce2");
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
            [
                Cards.Cross("start"),
                mid,
                OneRotate("turner"),
                lid,
                Cards.Tile("bounce1", Bounce, Bounce, Cards.Blank, Cards.Blank),
                bounce2,
            ]);

        var afterMid = See.Game(RepresentativeDeck.Play(game, 1, 0));
        Expect.Tile(afterMid, 1, 0, Oriented.Cross(0));

        var afterTurn = See.Game(RepresentativeDeck.Rotate(afterMid, 1, 0));
        Expect.Tile(afterTurn, 1, 0, Oriented.Cross(1));
        var afterTurner = See.Game(RepresentativeDeck.Play(afterTurn, 2, 0));

        var stackResult = RepresentativeDeck.Play(afterTurner, 1, 0);
        var afterLid = See.Game(stackResult);
        Assert.That(((TilePlaced)stackResult.Events[0]).Covered, Is.EqualTo(new TileId("mid")));
        Assert.That(See.Ids(afterLid.CoveredTileIds(1, 0)), Is.EqualTo(new[] { "mid" }));
        Assert.That(afterLid.TileCount, Is.EqualTo(3));

        // The first bounce peels the stack's top and reveals "mid" with the cells it had when covered (already
        // turned), not the cells its own placement orientation would show.
        var firstBounce = RepresentativeDeck.Bounce(afterLid, 1, 0);
        var afterFirstBounce = See.Game(firstBounce);
        Assert.That(
            firstBounce.Events,
            Is.EqualTo(new[] { new TileBounced(Cards.Seat("b"), 1, 0, new TileId("lid"), new TileId("mid")) }));
        Expect.Tile(afterFirstBounce, 1, 0, Oriented.Cross(1));
        Assert.That(afterFirstBounce.CoveredTileIds(1, 0), Is.Empty);
        Assert.That(afterFirstBounce.TileCount, Is.EqualTo(3), "a stacked bounce does not change the tile count");

        // The second bounce, in the same turn, removes the now single-layer position entirely.
        var secondBounce = RepresentativeDeck.Bounce(afterFirstBounce, 1, 0);
        var afterSecondBounce = See.Game(secondBounce);
        Assert.That(
            secondBounce.Events,
            Is.EqualTo(new[] { new TileBounced(Cards.Seat("b"), 1, 0, new TileId("mid"), null) }));
        Assert.That(afterSecondBounce.HasTileAt(1, 0), Is.False);
        Assert.That(afterSecondBounce.TileCount, Is.EqualTo(2));
        Assert.That(afterSecondBounce.MatchDeckRemaining, Is.EqualTo(4));

        var afterBounce1 = See.Game(RepresentativeDeck.Play(afterSecondBounce, 1, 0));
        Assert.That(afterBounce1.PendingMatchTile, Is.SameAs(bounce2));
        var afterBounce2 = See.Game(RepresentativeDeck.Play(afterBounce1, 0, 1));
        Assert.That(afterBounce2.PendingMatchTile, Is.SameAs(lid), "lid was bounced before mid");
        var afterLidRedrawn = See.Game(RepresentativeDeck.Play(afterBounce2, 1, 1));
        Assert.That(afterLidRedrawn.PendingMatchTile, Is.SameAs(mid), "mid trails lid, not swapped ahead of it");
    }

    [Test]
    public void ABounceOnALaterTurn_RemovesTheRevealedTile_AfterTheStackWasPeeled()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
            [
                Cards.Cross("start"),
                Cards.BlankTile("mid"),
                OneStack("lid"),
                OneBounce("bounce1"),
                OneBounce("bounce2"),
            ]);
        var afterMid = See.Game(RepresentativeDeck.Play(game, 1, 0));
        var afterLid = See.Game(RepresentativeDeck.Play(afterMid, 1, 0));

        var afterFirstBounce = See.Game(RepresentativeDeck.Bounce(afterLid, 1, 0));
        var afterBounce1 = See.Game(RepresentativeDeck.Play(afterFirstBounce, 2, 0));
        var secondBounce = RepresentativeDeck.Bounce(afterBounce1, 1, 0);

        Assert.That(
            secondBounce.Events,
            Is.EqualTo(new[] { new TileBounced(Cards.Seat("b"), 1, 0, new TileId("mid"), null) }));
        Assert.That(See.Game(secondBounce).HasTileAt(1, 0), Is.False);
        Assert.That(afterFirstBounce.HasTileAt(1, 0), Is.True);
    }

    [Test]
    public void ABouncedPosition_IsEmpty_SoThePlacementMayTakeIt()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"), Cards.BlankTile("side"), OneBounce("drawn"));
        var afterSide = See.Game(RepresentativeDeck.Play(game, 1, 0));

        var afterBounce = See.Game(RepresentativeDeck.Bounce(afterSide, 1, 0));
        var placed = RepresentativeDeck.Play(afterBounce, 1, 0);

        Assert.That(((TilePlaced)placed.Events.Single()).Covered, Is.Null);
        Assert.That(See.Game(placed).TileCount, Is.EqualTo(2));
        Assert.That(See.Game(placed).CellAt(2, 0), Is.EqualTo(Bounce));
    }

    [Test]
    public void BounceTheOnlyTile_EmptiesTheBoard_AndThePlacementIsLegalOnlyAtTheOrigin()
    {
        var start = Cards.Cross("start");
        var game = RepresentativeDeck.TwoSeats(
            start,
            Cards.Tile("drawn", Bounce, Bounce, Cards.Blank, Cards.Blank),
            Cards.BlankTile("next"));

        var bounced = RepresentativeDeck.Bounce(game, 0, 0);
        var empty = See.Game(bounced);

        Assert.That(bounced.Events, Is.EqualTo(new[] { new TileBounced(Cards.Seat("a"), 0, 0, new TileId("start"), null) }));
        Assert.That(empty.TileCount, Is.EqualTo(0));
        Assert.That(empty.HasTileAt(0, 0), Is.False);
        Assert.That(empty.CellAt(0, 0), Is.Null);
        Assert.That(empty.PendingMatchTile!.Id.Value, Is.EqualTo("drawn"));
        Assert.That(empty.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Expect.Rejected(empty, RepresentativeDeck.Try(empty, new UseBounce(0, 0)), RejectionReason.NoTileToBounce);
        Expect.Rejected(empty, RepresentativeDeck.Try(empty, new UseRotate(0, 0, 1)), RejectionReason.NoUseRemaining);

        foreach (var (x, y) in new[] { (1, 0), (0, 1), (-1, 0), (1, 1) })
        {
            Expect.Rejected(empty, RepresentativeDeck.Try(empty, new Place(x, y, 0)), RejectionReason.NotAtOrigin);
        }

        var placed = RepresentativeDeck.Play(empty, 0, 0, 2);
        var next = See.Game(placed);

        Assert.That(next.TileCount, Is.EqualTo(1));
        Assert.That(next.HasTileAt(0, 0), Is.True);
        Assert.That(((TilePlaced)placed.Events.Single()).Covered, Is.Null);
        Assert.That(next.PendingMatchTile!.Id.Value, Is.EqualTo("next"));
        Assert.That(next.MatchDeckRemaining, Is.EqualTo(2));
    }

    [Test]
    public void EmptyMatchDeck_ThrowsUnresolvedRuling_AndLeavesTheGame()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"));
        var handA = See.Ids(game.Hand(Cards.Seat("a")));
        var handB = See.Ids(game.Hand(Cards.Seat("b")));

        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(
            () => { RepresentativeDeck.Try(game, new UseBounce(0, 0)); },
            Throws.TypeOf<UnresolvedRulingException>().With.Message.EqualTo(
                "The match deck has no tile to place. Exhausting the match deck is an open ruling, so this command was not applied."));

        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(See.Ids(game.Hand(Cards.Seat("a"))), Is.EqualTo(handA));
        Assert.That(See.Ids(game.Hand(Cards.Seat("b"))), Is.EqualTo(handB));
    }

    // The square's four corners are (1,1), (2,1), (1,2), and (2,2), one cell from each of four tiles (see
    // SquareClaimTests). Bouncing "start" away before the finish is placed leaves the placement one corner short.
    [Test]
    public void ABounceThatRemovesAPatternCorner_BeforeThePlacement_PreventsTheClaim()
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

        var bounced = See.Game(RepresentativeDeck.Bounce(afterB, 0, 0));
        var placed = RepresentativeDeck.Play(bounced, 1, 1);
        var next = See.Game(placed);

        Assert.That(next.HasTileAt(0, 0), Is.False, "the removed corner leaves the board");
        Assert.That(next.CellAt(2, 2), Is.EqualTo(Cards.Red), "the placed tile still wrote its own corner");
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty, "the square never completed: bounce removed a corner");
        Assert.That(placed.Events, Has.Count.EqualTo(1));
        Assert.That(placed.Events[0], Is.TypeOf<TilePlaced>());
        Assert.That(next.TileCount, Is.EqualTo(3));
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
    }

    // "start" is a solid green square: dealt, all four of its cells already match the square pattern, but Start
    // never evaluates claims. Covering it hides the match; bouncing the cover away on a later turn exposes it,
    // yet the placement that follows writes none of the square's own four cells, so nothing is claimed.
    [Test]
    public void ABounceThatExposesAPattern_ThePlacedCellsDoNotComplete_DoesNotClaim()
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
        var stacked = See.Game(RepresentativeDeck.Play(afterPad, 0, 0));
        Assert.That(stacked.CellAt(0, 0), Is.EqualTo(Stack));
        Assert.That(stacked.Claims(Cards.Seat("a")), Is.Empty);

        var bounce = RepresentativeDeck.Bounce(stacked, 0, 0);
        var exposed = See.Game(bounce);
        var placed = RepresentativeDeck.Play(exposed, 2, 0);
        var next = See.Game(placed);

        Assert.That(
            bounce.Events,
            Is.EqualTo(new[] { new TileBounced(Cards.Seat("a"), 0, 0, new TileId("cover"), new TileId("start")) }));
        Assert.That(next.CellAt(0, 0), Is.EqualTo(Green));
        Assert.That(next.CellAt(1, 0), Is.EqualTo(Green));
        Assert.That(next.CellAt(0, 1), Is.EqualTo(Green));
        Assert.That(next.CellAt(1, 1), Is.EqualTo(Green));
        Assert.That(next.CoveredTileIds(0, 0), Is.Empty);
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty, "the placement writes none of the square's own cells");
        Assert.That(placed.Events, Has.Count.EqualTo(1));
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
    }

    private static Tile OneBounce(string id) => Cards.Tile(id, Bounce, Cards.Blank, Cards.Blank, Cards.Blank);

    private static Tile OneRotate(string id) => Cards.Tile(id, Rotate, Cards.Blank, Cards.Blank, Cards.Blank);

    private static Tile OneStack(string id) => Cards.Tile(id, Stack, Cards.Blank, Cards.Blank, Cards.Blank);

}
