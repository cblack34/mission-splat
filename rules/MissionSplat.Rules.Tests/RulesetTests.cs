namespace MissionSplat.Rules.Tests;

// The engine answers from a board, the drawn tile, the spent uses, and a hand alone: no match is dealt here.
[TestFixture]
public sealed class RulesetTests
{
    private static readonly Ruleset Ordinary =
        new(OrdinaryCatalog.NonScoringSymbols, OrdinaryCatalog.Patterns);

    private static readonly Cell Rotate = Cell.Symbol(OrdinaryCatalog.Rotate);

    private static readonly Cell Stack = Cell.Symbol(OrdinaryCatalog.Stack);

    [Test]
    public void APlacementNeedsAFullSharedSideOrAStackOnTheOccupiedPosition()
    {
        var board = Grid.FromStart(Cards.BlankTile("start"));
        var next = Cards.BlankTile("next");

        Assert.That(Ordinary.PlacementRefusal(board, next, 1, 0), Is.Null);
        Assert.That(Ordinary.PlacementRefusal(board, next, 1, 1)?.Reason, Is.EqualTo(RejectionReason.DoesNotShareFullSide));
        Assert.That(Ordinary.PlacementRefusal(board, next, 0, 0)?.Reason, Is.EqualTo(RejectionReason.CellOccupied));
    }

    [Test]
    public void OccupiedPositionIsStackableOnlyWhenTheDrawnTileShowsAStackInPlay()
    {
        var board = Grid.FromStart(Cards.BlankTile("start"));

        Assert.That(Ordinary.PlacementRefusal(board, Cards.Showing("s", Stack), 0, 0), Is.Null);
        Assert.That(Ordinary.KindAt(board, 0, 0), Is.EqualTo(PlacementKind.OnTop));
        Assert.That(Ordinary.KindAt(board, 1, 0), Is.EqualTo(PlacementKind.Beside));

        var noStack = new Ruleset([OrdinaryCatalog.Blank, OrdinaryCatalog.Rotate], OrdinaryCatalog.Patterns);
        Assert.That(noStack.PlacementRefusal(board, Cards.Showing("s", Stack), 0, 0)?.Reason, Is.EqualTo(RejectionReason.CellOccupied));
    }

    [Test]
    public void AnOccupiedBoardOffersEachTileAndItsFourNeighborsInOrder()
    {
        var board = Grid.FromStart(Cards.BlankTile("start"));

        var offered = Ordinary.LegalPlacements(board, Cards.BlankTile("next"));

        Assert.That(
            offered.Select(p => new BoardPosition(p.TileX, p.TileY)),
            Is.EqualTo(Agreement.At((-1, 0), (0, -1), (0, 1), (1, 0))));
        Assert.That(offered.All(p => p.Kind == PlacementKind.Beside), Is.True);
    }

    [Test]
    public void ASurroundedTileCannotBeRotatedButMayBeBounced()
    {
        var board = Grid.FromStart(Cards.BlankTile("c"));
        foreach (var (x, y, id) in new[] { (1, 0, "e"), (-1, 0, "w"), (0, 1, "n"), (0, -1, "s") })
        {
            var tile = Cards.BlankTile(id);
            board = board.Place(x, y, tile, tile.CellsAt(x, y, 0));
        }

        var drawn = Cards.Tile("d", Rotate, Cell.Symbol(OrdinaryCatalog.Bounce), Cards.Blank, Cards.Blank);

        Assert.That(
            Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Rotate, default, 0, 0)?.Reason,
            Is.EqualTo(RejectionReason.TileSurrounded));
        Assert.That(Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Bounce, default, 0, 0), Is.Null);
        Assert.That(
            Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Rotate, default, 5, 5)?.Reason,
            Is.EqualTo(RejectionReason.NoTileToRotate));
    }

    [Test]
    public void ASpentChargeRefusesTheUseWithItsOwnMessage()
    {
        var board = Grid.FromStart(Cards.BlankTile("start"));
        var drawn = Cards.Showing("r", Rotate);

        Assert.That(Ordinary.Remaining(drawn, OrdinaryCatalog.Rotate, default), Is.EqualTo(1));
        var refusal = Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Rotate, default(SpentUses).WithUse(OrdinaryCatalog.Rotate), 0, 0);
        Assert.That(refusal?.Reason, Is.EqualTo(RejectionReason.NoUseRemaining));
        Assert.That(refusal?.Message, Is.EqualTo("The drawn tile has no unused rotate cell."));
        Assert.That(
            Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Bounce, default, 0, 0)?.Message,
            Is.EqualTo("The drawn tile has no unused bounce cell."));
    }

    [Test]
    public void AnOmittedPowerGrantsNoUseAndDoesNotCountAsMixed()
    {
        var withoutRotate = new Ruleset([OrdinaryCatalog.Blank, OrdinaryCatalog.Stack], OrdinaryCatalog.Patterns);
        var mixed = Cards.Tile("m", Rotate, Stack, Cards.Blank, Cards.Blank);

        Assert.That(withoutRotate.Remaining(mixed, OrdinaryCatalog.Rotate, default), Is.EqualTo(0));
        Assert.That(withoutRotate.ShowsMixedPowers(mixed), Is.False);
        Assert.That(Ordinary.ShowsMixedPowers(mixed), Is.True);
    }

    [Test]
    public void ACompletionNeedsAnActivePatternAndAWrittenCell()
    {
        var purple = Cell.Color(OrdinaryCatalog.Purple);
        var row = Cards.Purple("row");
        var board = Grid.FromStart(Cards.Solid("start", purple));
        var east = Cards.Solid("east", purple);
        board = board.Place(1, 0, east, east.CellsAt(1, 0, 0));

        Assert.That(Ordinary.CompletedMissions(board, Written(3, 0), [row]), Is.EqualTo(new[] { row }));
        Assert.That(Ordinary.CompletedMissions(board, Written(9, 9), [row]), Is.Empty);

        var squaresOnly = new Ruleset(OrdinaryCatalog.NonScoringSymbols, [MissionPattern.Square]);
        Assert.That(squaresOnly.CompletedMissions(board, Written(3, 0), [row]), Is.Empty);
    }

    [Test]
    public void ABouncedBoardCanBeEmptyAndThenTakesOnlyTheOrigin()
    {
        var empty = Grid.FromStart(Cards.BlankTile("start")).Bounce(0, 0).Grid;
        var next = Cards.BlankTile("next");

        Assert.That(empty.TileCount, Is.EqualTo(0));
        Assert.That(Ordinary.LegalPlacements(empty, next), Is.EqualTo(new[] { new LegalPlacement(0, 0, PlacementKind.Beside) }));
        Assert.That(Ordinary.PlacementRefusal(empty, next, 1, 0)?.Reason, Is.EqualTo(RejectionReason.NotAtOrigin));
        Assert.That(Ordinary.PlacementRefusal(empty, next, 0, 0), Is.Null);
    }

    [Test]
    public void ABounceNeedsATileAtTheTarget()
    {
        var board = Grid.FromStart(Cards.BlankTile("start"));
        var drawn = Cards.Showing("b", Cell.Symbol(OrdinaryCatalog.Bounce));

        Assert.That(
            Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Bounce, default, 4, 4)?.Reason,
            Is.EqualTo(RejectionReason.NoTileToBounce));
    }

    [Test]
    public void OnlyRotateAndBounceAreUses()
    {
        var board = Grid.FromStart(Cards.BlankTile("start"));
        var drawn = Cards.Showing("s", Stack);

        Assert.Throws<ArgumentOutOfRangeException>(() => Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Stack, default, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Blank, default, 0, 0));
    }

    [Test]
    public void LegalTargetsListOnlyUsableTilesInOrderAndNoneOnceSpent()
    {
        var board = Grid.FromStart(Cards.BlankTile("c"));
        var east = Cards.BlankTile("e");
        board = board.Place(1, 0, east, east.CellsAt(1, 0, 0));
        var drawn = Cards.Showing("r", Rotate);

        Assert.That(
            Ordinary.LegalTargets(board, drawn, OrdinaryCatalog.Rotate, default),
            Is.EqualTo(Agreement.At((0, 0), (1, 0))));
        Assert.That(
            Ordinary.LegalTargets(board, drawn, OrdinaryCatalog.Rotate, default(SpentUses).WithUse(OrdinaryCatalog.Rotate)),
            Is.Empty);
    }

    private static (int X, int Y, Cell Value)[] Written(int x, int y) => [(x, y, Cell.Color(OrdinaryCatalog.Purple))];
}
