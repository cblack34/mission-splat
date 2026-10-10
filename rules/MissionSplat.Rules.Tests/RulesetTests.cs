namespace MissionSplat.Rules.Tests;

// The engine answers from a board, the drawn tile, the spent uses, and a hand alone: no match is dealt here.
[TestFixture]
public sealed class RulesetTests
{
    private static readonly Ruleset Ordinary =
        new(OrdinaryCatalog.NonScoringSymbols, OrdinaryCatalog.Patterns);

    private static readonly Cell Rotate = Cell.Symbol(OrdinaryCatalog.Rotate);

    private static readonly Cell Stack = Cell.Symbol(OrdinaryCatalog.Stack);

    private static Tile Plain(string id) => Cards.BlankTile(id);

    private static Tile Showing(string id, Cell power) => Cards.Tile(id, power, Cards.Blank, Cards.Blank, Cards.Blank);

    [Test]
    public void APlacementNeedsAFullSharedSideOrAStackOnTheOccupiedPosition()
    {
        var board = Grid.FromStart(Plain("start"));
        var next = Plain("next");

        Assert.That(Ordinary.PlacementRefusal(board, next, 1, 0), Is.Null);
        Assert.That(Ordinary.PlacementRefusal(board, next, 1, 1)?.Reason, Is.EqualTo(RejectionReason.DoesNotShareFullSide));
        Assert.That(Ordinary.PlacementRefusal(board, next, 0, 0)?.Reason, Is.EqualTo(RejectionReason.CellOccupied));
    }

    [Test]
    public void OccupiedPositionIsStackableOnlyWhenTheDrawnTileShowsAStackInPlay()
    {
        var board = Grid.FromStart(Plain("start"));

        Assert.That(Ordinary.PlacementRefusal(board, Showing("s", Stack), 0, 0), Is.Null);
        Assert.That(Ordinary.KindAt(board, 0, 0), Is.EqualTo(PlacementKind.OnTop));
        Assert.That(Ordinary.KindAt(board, 1, 0), Is.EqualTo(PlacementKind.Beside));

        var noStack = new Ruleset([OrdinaryCatalog.Blank, OrdinaryCatalog.Rotate], OrdinaryCatalog.Patterns);
        Assert.That(noStack.PlacementRefusal(board, Showing("s", Stack), 0, 0)?.Reason, Is.EqualTo(RejectionReason.CellOccupied));
    }

    [Test]
    public void AnOccupiedBoardOffersEachTileAndItsFourNeighborsInOrder()
    {
        var board = Grid.FromStart(Plain("start"));

        Assert.That(
            Ordinary.PlacementCandidates(board),
            Is.EqualTo(Agreement.At((-1, 0), (0, -1), (0, 0), (0, 1), (1, 0))));
    }

    [Test]
    public void ASurroundedTileCannotBeRotatedButMayBeBounced()
    {
        var board = Grid.FromStart(Plain("c"));
        foreach (var (x, y, id) in new[] { (1, 0, "e"), (-1, 0, "w"), (0, 1, "n"), (0, -1, "s") })
        {
            var tile = Plain(id);
            board = board.Place(x, y, tile, tile.CellsAt(x, y, 0));
        }

        var drawn = Cards.Tile("d", Rotate, Cell.Symbol(OrdinaryCatalog.Bounce), Cards.Blank, Cards.Blank);

        Assert.That(
            Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Rotate, 0, 0, 0)?.Reason,
            Is.EqualTo(RejectionReason.TileSurrounded));
        Assert.That(Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Bounce, 0, 0, 0), Is.Null);
        Assert.That(
            Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Rotate, 0, 5, 5)?.Reason,
            Is.EqualTo(RejectionReason.NoTileToRotate));
    }

    [Test]
    public void ASpentChargeRefusesTheUseWithItsOwnMessage()
    {
        var board = Grid.FromStart(Plain("start"));
        var drawn = Showing("r", Rotate);

        Assert.That(Ordinary.Remaining(drawn, OrdinaryCatalog.Rotate, 0), Is.EqualTo(1));
        var refusal = Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Rotate, 1, 0, 0);
        Assert.That(refusal?.Reason, Is.EqualTo(RejectionReason.NoUseRemaining));
        Assert.That(refusal?.Message, Is.EqualTo("The drawn tile has no unused rotate cell."));
        Assert.That(
            Ordinary.UseRefusal(board, drawn, OrdinaryCatalog.Bounce, 0, 0, 0)?.Message,
            Is.EqualTo("The drawn tile has no unused bounce cell."));
    }

    [Test]
    public void AnOmittedPowerGrantsNoUseAndDoesNotCountAsMixed()
    {
        var withoutRotate = new Ruleset([OrdinaryCatalog.Blank, OrdinaryCatalog.Stack], OrdinaryCatalog.Patterns);
        var mixed = Cards.Tile("m", Rotate, Stack, Cards.Blank, Cards.Blank);

        Assert.That(withoutRotate.Remaining(mixed, OrdinaryCatalog.Rotate, 0), Is.EqualTo(0));
        Assert.That(withoutRotate.ShowsMixedPowers(mixed), Is.False);
        Assert.That(Ordinary.ShowsMixedPowers(mixed), Is.True);
    }

    [Test]
    public void ACompletionNeedsAnActivePatternAndAWrittenCell()
    {
        var purple = Cell.Color(OrdinaryCatalog.Purple);
        var row = Cards.Purple("row");
        var cells = new Dictionary<CellCoord, Cell>
        {
            [new CellCoord(0, 0)] = purple,
            [new CellCoord(1, 0)] = purple,
            [new CellCoord(2, 0)] = purple,
            [new CellCoord(3, 0)] = purple,
        };

        Assert.That(Ordinary.CompletedMissions([row], cells, [new CellCoord(3, 0)]), Is.EqualTo(new[] { row }));
        Assert.That(Ordinary.CompletedMissions([row], cells, [new CellCoord(9, 9)]), Is.Empty);

        var squaresOnly = new Ruleset(OrdinaryCatalog.NonScoringSymbols, [MissionPattern.Square]);
        Assert.That(squaresOnly.CompletedMissions([row], cells, [new CellCoord(3, 0)]), Is.Empty);
    }
}
