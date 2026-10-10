namespace MissionSplat.Rules.Tests;

public class PlacementTests
{
    [TestCase(1, 1)]
    [TestCase(1, -1)]
    [TestCase(-1, 1)]
    [TestCase(-1, -1)]
    public void CornerOnlyPlacement_IsRejected_AndTheBoardIsUnchanged(int tileX, int tileY)
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"), Cards.BlankTile("drawn"));

        var result = RepresentativeDeck.Try(game, new Place(tileX, tileY, 0));

        Assert.That(result.IsAccepted, Is.False);
        Assert.That(result.Rejection?.Reason, Is.EqualTo(RejectionReason.DoesNotShareFullSide));
        Assert.That(result.Events, Is.Empty);
        Assert.That(result.Game, Is.SameAs(game));
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
    }

    [Test]
    public void WestFullSide_IsAccepted_AndCornersStayRejected()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile(
                "west",
                Cards.Red,
                Cards.Blue,
                Cell.Color(OrdinaryCatalog.Green),
                Cell.Color(OrdinaryCatalog.Purple)));

        foreach (var tileY in new[] { 1, -1 })
        {
            var corner = RepresentativeDeck.Try(game, new Place(-1, tileY, 0));
            Assert.That(corner.IsAccepted, Is.False);
            Assert.That(corner.Rejection?.Reason, Is.EqualTo(RejectionReason.DoesNotShareFullSide));
            Assert.That(corner.Events, Is.Empty);
            Assert.That(corner.Game, Is.SameAs(game));
            Assert.That(game.TileCount, Is.EqualTo(1));
        }

        var placed = See.Game(RepresentativeDeck.Play(game, -1, 0));

        Assert.That(placed.TileCount, Is.EqualTo(2));
        Assert.That(placed.HasTileAt(0, 0), Is.True);
        Assert.That(placed.HasTileAt(-1, 0), Is.True);
        Assert.That(placed.CellAt(-2, 0), Is.EqualTo(Cards.Red));
        Assert.That(placed.CellAt(-1, 0), Is.EqualTo(Cards.Blue));
        Assert.That(placed.CellAt(-2, 1), Is.EqualTo(Cell.Color(OrdinaryCatalog.Green)));
        Assert.That(placed.CellAt(-1, 1), Is.EqualTo(Cell.Color(OrdinaryCatalog.Purple)));
        Assert.That(placed.CellAt(0, 0), Is.EqualTo(Cards.Blank));
    }

    [Test]
    public void OccupiedCell_RejectsATileWithoutStack_AndAStackTileMayStillUseAFullSide()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile("plain", Cell.Color(OrdinaryCatalog.Green), Cards.Blank, Cards.Blank, Cards.Blank),
            Cards.Tile("stacked", Cell.Symbol(OrdinaryCatalog.Stack), Cards.Blank, Cards.Blank, Cards.Blank));

        var occupied = RepresentativeDeck.Try(game, new Place(0, 0, 0));

        Assert.That(occupied.IsAccepted, Is.False);
        Assert.That(occupied.Rejection?.Reason, Is.EqualTo(RejectionReason.CellOccupied));
        Assert.That(occupied.Events, Is.Empty);
        Assert.That(occupied.Game, Is.SameAs(game));
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(2));

        var beside = RepresentativeDeck.Play(game, 1, 0);
        var afterPlain = See.Game(beside);
        Assert.That(afterPlain.CellAt(2, 0), Is.EqualTo(Cell.Color(OrdinaryCatalog.Green)));
        Assert.That(afterPlain.TileCount, Is.EqualTo(2));

        var onSide = RepresentativeDeck.Play(afterPlain, 0, 1);
        var afterStack = See.Game(onSide);
        Assert.That(afterStack.CellAt(0, 2), Is.EqualTo(Cell.Symbol(OrdinaryCatalog.Stack)));
        Assert.That(afterStack.TileCount, Is.EqualTo(3));
        Assert.That(afterStack.HasTileAt(0, 1), Is.True);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void QuarterTurnsClockwise_MoveCellsAroundTheTile(int quarterTurns)
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Cross("spun"));

        var placed = See.Game(RepresentativeDeck.Play(game, 1, 0, quarterTurns));

        Expect.Tile(placed, 1, 0, Oriented.Cross(quarterTurns));
    }

    [TestCase(4)]
    [TestCase(-1)]
    public void QuarterTurnsOutsideZeroThroughThree_AreRejected(int quarterTurns)
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"), Cards.BlankTile("drawn"));

        CommandResult? result = null;
        Assert.That(() => { result = RepresentativeDeck.Try(game, new Place(1, 0, quarterTurns)); }, Throws.Nothing);

        Assert.That(result?.IsAccepted, Is.False);
        Assert.That(result?.Rejection?.Reason, Is.EqualTo(RejectionReason.InvalidQuarterTurns));
        Assert.That(result?.Game, Is.SameAs(game));
        Assert.That(game.TileCount, Is.EqualTo(1));
    }

    [Test]
    public void ApplyFromANonCurrentSeat_IsRejected_AndTheMatchIsUnchanged()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"), Cards.BlankTile("drawn"));

        foreach (GameAction action in new GameAction[] { new Place(1, 0, 0), new UseRotate(0, 0, 1), new UseBounce(0, 0) })
        {
            var result = game.Apply(Cards.Seat("b"), action);

            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.Rejection?.Reason, Is.EqualTo(RejectionReason.NotYourTurn));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Game, Is.SameAs(game));
        }

        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(game.PendingMatchTile!.Id.Value, Is.EqualTo("drawn"));
    }

    [Test]
    public void ApplyWithoutAnAction_Throws_AndLeavesTheGame()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"), Cards.BlankTile("drawn"));

        Assert.That(() => game.Apply(Cards.Seat("a"), null!), Throws.ArgumentNullException);
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
    }

    [Test]
    public void AcceptedPlace_EmitsOneTilePlaced_WithNothingCovered()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"), Cards.BlankTile("drawn"));

        var result = RepresentativeDeck.Play(game, 1, 0, 2);

        var placed = (TilePlaced)result.Events.Single();
        Assert.That(placed, Is.EqualTo(new TilePlaced(Cards.Seat("a"), new TileId("drawn"), 1, 0, 2, null)));
    }

    [Test]
    public void EmptyMatchDeck_ThrowsUnresolvedRuling_AndLeavesTheGame()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"));
        var handA = See.Ids(game.Hand(Cards.Seat("a")));
        var handB = See.Ids(game.Hand(Cards.Seat("b")));

        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(
            typeof(UnresolvedRulingException).IsSubclassOf(typeof(InvalidOperationException)),
            Is.False);
        Assert.That(
            () => { RepresentativeDeck.Try(game, new Place(1, 0, 0)); },
            Throws.TypeOf<UnresolvedRulingException>().With.Message.EqualTo(
                "The match deck has no tile to place. Exhausting the match deck is an open ruling, so this command was not applied."));

        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(See.Ids(game.Hand(Cards.Seat("a"))), Is.EqualTo(handA));
        Assert.That(See.Ids(game.Hand(Cards.Seat("b"))), Is.EqualTo(handB));
    }

}
