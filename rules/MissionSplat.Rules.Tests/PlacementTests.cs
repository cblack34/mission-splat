namespace MissionSplat.Rules.Tests;

public class PlacementTests
{
    [TestCase(1, 1)]
    [TestCase(1, -1)]
    [TestCase(-1, 1)]
    [TestCase(-1, -1)]
    public void CornerOnlyPlacement_IsRejected_AndTheBoardIsUnchanged(int tileX, int tileY)
    {
        var game = TwoSeatGame(Cards.BlankTile("start"), Cards.BlankTile("drawn"));

        var result = game.Place(tileX, tileY, 0);

        Assert.That(result.IsAccepted, Is.False);
        Assert.That(result.Rejection?.Reason, Is.EqualTo(RejectionReason.DoesNotShareFullSide));
        Assert.That(result.Events, Is.Empty);
        Assert.That(result.Game, Is.SameAs(game));
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
    }

    [Test]
    public void OccupiedCell_RejectsATileWithoutStack_AndAStackTileMayStillUseAFullSide()
    {
        var game = TwoSeatGame(
            Cards.BlankTile("start"),
            Cards.Tile("plain", Cell.Color(OrdinaryCatalog.Green), Cards.Blank, Cards.Blank, Cards.Blank),
            Cards.Tile("stacked", Cell.Symbol(OrdinaryCatalog.Stack), Cards.Blank, Cards.Blank, Cards.Blank));

        var occupied = game.Place(0, 0, 0);

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
        var game = TwoSeatGame(
            Cards.BlankTile("start"),
            Cards.Tile("spun", Cards.Red, Cards.Blue, Cell.Color(OrdinaryCatalog.Green), Cell.Color(OrdinaryCatalog.Purple)));

        var placed = See.Game(RepresentativeDeck.Play(game, 1, 0, quarterTurns));
        var expected = ExpectedClockwise(quarterTurns);

        Assert.That(placed.CellAt(2, 0), Is.EqualTo(expected[0]));
        Assert.That(placed.CellAt(3, 0), Is.EqualTo(expected[1]));
        Assert.That(placed.CellAt(2, 1), Is.EqualTo(expected[2]));
        Assert.That(placed.CellAt(3, 1), Is.EqualTo(expected[3]));
    }

    [TestCase(4)]
    [TestCase(-1)]
    public void QuarterTurnsOutsideZeroThroughThree_AreRejected(int quarterTurns)
    {
        var game = TwoSeatGame(Cards.BlankTile("start"), Cards.BlankTile("drawn"));

        CommandResult? result = null;
        Assert.That(() => { result = game.Place(1, 0, quarterTurns); }, Throws.Nothing);

        Assert.That(result?.IsAccepted, Is.False);
        Assert.That(result?.Rejection?.Reason, Is.EqualTo(RejectionReason.InvalidQuarterTurns));
        Assert.That(result?.Game, Is.SameAs(game));
        Assert.That(game.TileCount, Is.EqualTo(1));
    }

    [Test]
    public void OccupiedCell_RejectsAStackSymbol_AndTheTileStaysForAFullSide()
    {
        var game = TwoSeatGame(
            Cards.BlankTile("start"),
            Cards.Tile("stacked", Cell.Symbol(OrdinaryCatalog.Stack), Cards.Blank, Cards.Blank, Cards.Blank));

        var occupied = game.Place(0, 0, 0);

        Assert.That(occupied.IsAccepted, Is.False);
        Assert.That(occupied.Rejection?.Reason, Is.EqualTo(RejectionReason.CellOccupied));
        Assert.That(occupied.Events, Is.Empty);
        Assert.That(occupied.Game, Is.SameAs(game));
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Blank));
        Assert.That(game.CellAt(1, 0), Is.EqualTo(Cards.Blank));
        Assert.That(game.CellAt(0, 1), Is.EqualTo(Cards.Blank));
        Assert.That(game.CellAt(1, 1), Is.EqualTo(Cards.Blank));

        var beside = RepresentativeDeck.Play(game, 1, 0);
        var after = See.Game(beside);
        Assert.That(after.CellAt(2, 0), Is.EqualTo(Cell.Symbol(OrdinaryCatalog.Stack)));
        Assert.That(after.CellAt(0, 0), Is.EqualTo(Cards.Blank));
        Assert.That(after.CellAt(1, 0), Is.EqualTo(Cards.Blank));
        Assert.That(after.CellAt(0, 1), Is.EqualTo(Cards.Blank));
        Assert.That(after.CellAt(1, 1), Is.EqualTo(Cards.Blank));
        Assert.That(after.HasTileAt(0, 0), Is.True);
        Assert.That(after.HasTileAt(1, 0), Is.True);
        Assert.That(after.TileCount, Is.EqualTo(2));
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
            typeof(UnresolvedRulingException).IsSubclassOf(typeof(InvalidOperationException)),
            Is.False);
        Assert.That(
            () => { game.Place(1, 0, 0); },
            Throws.TypeOf<UnresolvedRulingException>().With.Message.EqualTo(
                "The match deck has no tile to place. Exhausting the match deck is an open ruling, so this command was not applied."));

        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(See.Ids(game.Hand(Cards.Seat("a"))), Is.EqualTo(handA));
        Assert.That(See.Ids(game.Hand(Cards.Seat("b"))), Is.EqualTo(handB));
    }

    // Y increases upward, so one clockwise turn moves bottom-left (index 0) to top-left (index 2).
    private static Cell[] ExpectedClockwise(int quarterTurns) => quarterTurns switch
    {
        0 => [Cards.Red, Cards.Blue, Cell.Color(OrdinaryCatalog.Green), Cell.Color(OrdinaryCatalog.Purple)],
        1 => [Cards.Blue, Cell.Color(OrdinaryCatalog.Purple), Cards.Red, Cell.Color(OrdinaryCatalog.Green)],
        2 => [Cell.Color(OrdinaryCatalog.Purple), Cell.Color(OrdinaryCatalog.Green), Cards.Blue, Cards.Red],
        3 => [Cell.Color(OrdinaryCatalog.Green), Cards.Red, Cell.Color(OrdinaryCatalog.Purple), Cards.Blue],
        _ => throw new ArgumentOutOfRangeException(nameof(quarterTurns)),
    };

    private static Game TwoSeatGame(params Tile[] tiles)
    {
        return RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
            tiles);
    }
}
