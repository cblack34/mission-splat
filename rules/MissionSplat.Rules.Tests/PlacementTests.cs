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

    [Test]
    public void QuarterTurnsOutsideZeroThroughThree_AreRejected()
    {
        var game = TwoSeatGame(Cards.BlankTile("start"), Cards.BlankTile("drawn"));

        var result = game.Place(1, 0, 4);

        Assert.That(result.IsAccepted, Is.False);
        Assert.That(result.Rejection?.Reason, Is.EqualTo(RejectionReason.InvalidQuarterTurns));
        Assert.That(result.Game, Is.SameAs(game));
        Assert.That(game.TileCount, Is.EqualTo(1));
    }

    private static Cell[] ExpectedClockwise(int quarterTurns) => quarterTurns switch
    {
        0 => [Cards.Red, Cards.Blue, Cell.Color(OrdinaryCatalog.Green), Cell.Color(OrdinaryCatalog.Purple)],
        1 => [Cell.Color(OrdinaryCatalog.Green), Cards.Red, Cell.Color(OrdinaryCatalog.Purple), Cards.Blue],
        2 => [Cell.Color(OrdinaryCatalog.Purple), Cell.Color(OrdinaryCatalog.Green), Cards.Blue, Cards.Red],
        3 => [Cards.Blue, Cell.Color(OrdinaryCatalog.Purple), Cards.Red, Cell.Color(OrdinaryCatalog.Green)],
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
