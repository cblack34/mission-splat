namespace MissionSplat.Rules.Tests;

// A rotate, stack, or bounce that setup does not list is read as a blank: no score, no use, no on-top placement.
public class OmittedPowerTests
{
    private static readonly Cell Rotate = Cell.Symbol(OrdinaryCatalog.Rotate);

    private static readonly Cell Stack = Cell.Symbol(OrdinaryCatalog.Stack);

    private static readonly Cell Bounce = Cell.Symbol(OrdinaryCatalog.Bounce);

    [Test]
    public void OmittedRotate_GrantsNoUse_AndTheListedSymbolGrantsIt()
    {
        var tiles = new[] { Cards.BlankTile("start"), Cards.Tile("drawn", Rotate, Cards.Blank, Cards.Blank, Cards.Blank) };

        var omitted = Open(OrdinaryCatalog.Rotate, tiles);
        var refused = RepresentativeDeck.Try(omitted, new UseRotate(0, 0, 1));
        var listed = Open(null, tiles);

        Expect.Rejected(omitted, refused, RejectionReason.NoUseRemaining);
        Assert.That(RepresentativeDeck.Try(listed, new UseRotate(0, 0, 1)).IsAccepted, Is.True);
    }

    [Test]
    public void OmittedBounce_GrantsNoUse_AndTheListedSymbolGrantsIt()
    {
        var tiles = new[] { Cards.BlankTile("start"), Cards.Tile("drawn", Bounce, Cards.Blank, Cards.Blank, Cards.Blank) };

        var omitted = Open(OrdinaryCatalog.Bounce, tiles);
        var refused = RepresentativeDeck.Try(omitted, new UseBounce(0, 0));
        var listed = Open(null, tiles);

        Expect.Rejected(omitted, refused, RejectionReason.NoUseRemaining);
        Assert.That(RepresentativeDeck.Try(listed, new UseBounce(0, 0)).IsAccepted, Is.True);
    }

    [Test]
    public void OmittedStack_OffersNoOnTopPlacement_AndTheListedSymbolCoversTheTile()
    {
        var tiles = new[] { Cards.BlankTile("start"), Cards.Tile("drawn", Stack, Cards.Blank, Cards.Blank, Cards.Blank) };

        var omitted = Open(OrdinaryCatalog.Stack, tiles);
        var refused = RepresentativeDeck.Try(omitted, new Place(0, 0, 0));
        var listed = Open(null, tiles);
        var covered = RepresentativeDeck.Play(listed, 0, 0);

        Expect.Rejected(omitted, refused, RejectionReason.CellOccupied);
        Assert.That(((TilePlaced)covered.Events[0]).Covered, Is.EqualTo(new TileId("start")));
        Assert.That(RepresentativeDeck.Try(omitted, new Place(1, 0, 0)).IsAccepted, Is.True, "it still places beside");
    }

    [Test]
    public void PowersInPlay_ListsTheSetupsPowers_AndOmitsAnUnlistedOne()
    {
        var tiles = new[] { Cards.BlankTile("start"), Cards.BlankTile("drawn") };

        Assert.That(
            Open(null, tiles).PowersInPlay,
            Is.EqualTo(new[] { OrdinaryCatalog.Rotate, OrdinaryCatalog.Stack, OrdinaryCatalog.Bounce }));
        Assert.That(
            Open(OrdinaryCatalog.Stack, tiles).PowersInPlay,
            Is.EqualTo(new[] { OrdinaryCatalog.Rotate, OrdinaryCatalog.Bounce }));
    }

    [TestCase("rotate")]
    [TestCase("stack")]
    [TestCase("bounce")]
    public void AnOmittedPowerCell_BlocksAPatternLikeABlank(string power)
    {
        var omitted = new SymbolId(power);
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("spare"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("finisher", Cards.Red, Cell.Symbol(omitted), Cards.Blank, Cards.Blank),
            ],
            symbols: Without(omitted));

        var placed = See.Game(RepresentativeDeck.Play(game, 1, 0));

        Assert.That(placed.Claims(Cards.Seat("a")), Is.Empty);
    }

    private static Game Open(SymbolId? omitted, Tile[] tiles) =>
        RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
            tiles,
            symbols: omitted is { } symbol ? Without(symbol) : null);

    private static SymbolId[] Without(SymbolId omitted) =>
        OrdinaryCatalog.NonScoringSymbols.Where(symbol => !symbol.Equals(omitted)).ToArray();

}
