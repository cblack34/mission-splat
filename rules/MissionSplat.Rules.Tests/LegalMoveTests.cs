namespace MissionSplat.Rules.Tests;

// Every answer is checked against Apply by Agreement, so a query that drifts from the rules fails here.
public class LegalMoveTests
{
    private static readonly Cell Rotate = Cell.Symbol(OrdinaryCatalog.Rotate);

    private static readonly Cell Stack = Cell.Symbol(OrdinaryCatalog.Stack);

    private static readonly Cell Bounce = Cell.Symbol(OrdinaryCatalog.Bounce);

    private static readonly LegalPlacement[] FourSidesOfTheOrigin =
    [
        new(-1, 0, PlacementKind.Beside),
        new(0, -1, PlacementKind.Beside),
        new(0, 1, PlacementKind.Beside),
        new(1, 0, PlacementKind.Beside),
    ];

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void LegalPlacements_OnASingleTile_ListFourSidesBeside_AtEveryOrientation(int quarterTurns)
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"), Cards.Cross("drawn"));

        Assert.That(Agreement.Placements(game, quarterTurns), Is.EqualTo(FourSidesOfTheOrigin));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void LegalPlacements_ListTheOccupiedPositionOnTop_OnlyWhenTheDrawnTileShowsStack(int quarterTurns)
    {
        var plain = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"), Cards.BlankTile("drawn"));
        var stacking = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile("drawn", Cards.Blank, Cards.Blank, Cards.Blank, Stack));

        Assert.That(Agreement.Placements(plain, quarterTurns), Has.None.Matches<LegalPlacement>(p => p.Kind == PlacementKind.OnTop));
        Assert.That(
            Agreement.Placements(stacking, quarterTurns),
            Is.EqualTo(
                new LegalPlacement[]
                {
                    new(-1, 0, PlacementKind.Beside),
                    new(0, -1, PlacementKind.Beside),
                    new(0, 0, PlacementKind.OnTop),
                    new(0, 1, PlacementKind.Beside),
                    new(1, 0, PlacementKind.Beside),
                }));
    }

    [Test]
    public void LegalPlacements_ForAStackTileWhoseSymbolIsOmitted_ListNothingOnTop()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
            [Cards.BlankTile("start"), Cards.Tile("drawn", Stack, Cards.Blank, Cards.Blank, Cards.Blank)],
            symbols: [OrdinaryCatalog.Blank, OrdinaryCatalog.Rotate, OrdinaryCatalog.Bounce]);

        Assert.That(Agreement.Placements(game, 0), Is.EqualTo(FourSidesOfTheOrigin));
    }

    [Test]
    public void LegalPlacements_OnABiggerBoard_ExcludeCornersAndOccupiedPositions_AndListSharedSidesOnce()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.BlankTile("east"),
            Cards.BlankTile("north"),
            Cards.BlankTile("drawn"));
        var withTwoNeighbors = See.Game(RepresentativeDeck.Play(See.Game(RepresentativeDeck.Play(game, 1, 0)), 0, 1));

        Assert.That(
            Agreement.Placements(withTwoNeighbors, 0),
            Is.EqualTo(
                new LegalPlacement[]
                {
                    new(-1, 0, PlacementKind.Beside),
                    new(-1, 1, PlacementKind.Beside),
                    new(0, -1, PlacementKind.Beside),
                    new(0, 2, PlacementKind.Beside),
                    new(1, -1, PlacementKind.Beside),
                    new(1, 1, PlacementKind.Beside),
                    new(2, 0, PlacementKind.Beside),
                }));
    }

    [Test]
    public void LegalPlacements_AfterABounceEmptiesTheBoard_ListOnlyTheOrigin()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile("drawn", Bounce, Cards.Blank, Cards.Blank, Cards.Blank));

        var empty = See.Game(RepresentativeDeck.Bounce(game, 0, 0));

        Assert.That(empty.TileCount, Is.EqualTo(0));
        foreach (var quarterTurns in new[] { 0, 1, 2, 3 })
        {
            Assert.That(
                Agreement.Placements(empty, quarterTurns),
                Is.EqualTo(new[] { new LegalPlacement(0, 0, PlacementKind.Beside) }));
        }
    }

    [Test]
    public void LegalPlacements_AreEmpty_ForQuarterTurnsApplyRefuses_AndWhenNoTileIsPending()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"), Cards.BlankTile("drawn"));
        var drawnOut = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"));

        Assert.That(game.LegalPlacements(-1), Is.Empty);
        Assert.That(game.LegalPlacements(4), Is.Empty);
        Assert.That(drawnOut.PendingMatchTile, Is.Null);
        Assert.That(drawnOut.LegalPlacements(0), Is.Empty);
    }

    [Test]
    public void LegalPlacements_AreEmpty_OnceTheGameIsWon()
    {
        var won = See.Game(
            RepresentativeDeck.Play(
                RepresentativeDeck.Start(
                    ["a", "b"],
                    "a",
                    [
                        Cards.Mission("red-square", MissionPattern.Square, OrdinaryCatalog.Red),
                        Cards.Purple("a2"),
                        Cards.Purple("b1"),
                        Cards.Purple("b2"),
                        Cards.Purple("spare"),
                    ],
                    [Cards.BlankTile("start"), Cards.Solid("next", Cards.Red), Cards.BlankTile("later")],
                    claimsRequiredToWin: 1),
                1,
                0));

        Assert.That(won.HasEnded, Is.True);
        Assert.That(won.LegalPlacements(0), Is.Empty);
        Assert.That(won.LegalTargets(OrdinaryCatalog.Bounce), Is.Empty);
        Assert.That(won.RemainingUses(OrdinaryCatalog.Bounce), Is.EqualTo(0));
    }

    [Test]
    public void LegalPlacements_AfterAStack_StillListTheCoveredPositionOnTop()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile("lid", Stack, Cards.Blank, Cards.Blank, Cards.Blank),
            Cards.Tile("second-lid", Stack, Cards.Blank, Cards.Blank, Cards.Blank));

        var covered = See.Game(RepresentativeDeck.Play(game, 0, 0));

        Assert.That(
            Agreement.Placements(covered, 2),
            Is.EqualTo(
                new LegalPlacement[]
                {
                    new(-1, 0, PlacementKind.Beside),
                    new(0, -1, PlacementKind.Beside),
                    new(0, 0, PlacementKind.OnTop),
                    new(0, 1, PlacementKind.Beside),
                    new(1, 0, PlacementKind.Beside),
                }));
    }

    [Test]
    public void LegalTargets_ForBounce_ListEveryBoardTile_InOrder()
    {
        var game = SurroundedCenter(Bounce);

        Assert.That(
            Agreement.Targets(game, OrdinaryCatalog.Bounce),
            Is.EqualTo(Agreement.At((-1, 0), (0, -1), (0, 0), (0, 1), (1, 0))));
    }

    [Test]
    public void LegalTargets_ForRotate_LeaveOutASurroundedTile()
    {
        var game = SurroundedCenter(Rotate);

        Assert.That(
            Agreement.Targets(game, OrdinaryCatalog.Rotate),
            Is.EqualTo(Agreement.At((-1, 0), (0, -1), (0, 1), (1, 0))));
    }

    [Test]
    public void LegalTargets_ListAStackedPositionOnce()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile("lid", Stack, Cards.Blank, Cards.Blank, Cards.Blank),
            Cards.Tile("drawn", Rotate, Cards.Blank, Cards.Blank, Cards.Blank));

        var covered = See.Game(RepresentativeDeck.Play(game, 0, 0));

        Assert.That(Agreement.Targets(covered, OrdinaryCatalog.Rotate), Is.EqualTo(Agreement.At((0, 0))));
    }

    [Test]
    public void LegalTargets_AreEmpty_ForASymbolThatIsNotARotateOrBounce_OrWhenTheDrawnTileLacksIt()
    {
        var onlyBounce = SurroundedCenter(Bounce);

        Assert.That(Agreement.Targets(onlyBounce, OrdinaryCatalog.Rotate), Is.Empty);
        Assert.That(Agreement.Targets(onlyBounce, OrdinaryCatalog.Stack), Is.Empty);
        Assert.That(Agreement.Targets(onlyBounce, OrdinaryCatalog.Blank), Is.Empty);
        Assert.That(Agreement.Targets(onlyBounce, new SymbolId("unlisted")), Is.Empty);
    }

    [Test]
    public void LegalTargets_AreEmpty_OnceEveryUseOfThePowerIsSpent()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.Cross("start"),
            Cards.Tile("drawn", Rotate, Bounce, Cards.Blank, Cards.Blank));

        var rotated = See.Game(RepresentativeDeck.Rotate(game, 0, 0));

        Assert.That(Agreement.Targets(rotated, OrdinaryCatalog.Rotate), Is.Empty);
        Assert.That(Agreement.Targets(rotated, OrdinaryCatalog.Bounce), Is.EqualTo(Agreement.At((0, 0))));
    }

    [TestCase("rotate")]
    [TestCase("bounce")]
    public void LegalTargets_AreEmpty_ForAnOmittedPower_AndListedWhenTheSymbolIsInPlay(string power)
    {
        var symbol = new SymbolId(power);
        var drawn = Cards.Tile("drawn", Cell.Symbol(symbol), Cards.Blank, Cards.Blank, Cards.Blank);
        Game Open(IReadOnlyList<SymbolId>? symbols) =>
            RepresentativeDeck.Start(
                ["a", "b"],
                "a",
                [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
                [Cards.Cross("start"), drawn],
                symbols: symbols);

        var omitted = Open(OrdinaryCatalog.NonScoringSymbols.Where(s => !s.Equals(symbol)).ToArray());
        var listed = Open(null);

        Assert.That(Agreement.Targets(omitted, symbol), Is.Empty);
        Assert.That(omitted.RemainingUses(symbol), Is.EqualTo(0));
        Assert.That(Agreement.Targets(listed, symbol), Is.EqualTo(Agreement.At((0, 0))));
        Assert.That(listed.RemainingUses(symbol), Is.EqualTo(1));
    }

    [Test]
    public void LegalTargets_AreEmpty_WhenNoTileIsPending()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"));

        Assert.That(game.LegalTargets(OrdinaryCatalog.Rotate), Is.Empty);
        Assert.That(game.LegalTargets(OrdinaryCatalog.Bounce), Is.Empty);
    }

    [Test]
    public void RemainingUses_CountTheCellsOnTheDrawnTile_AndFallAsEachUseIsSpent()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.Cross("start"),
            Cards.Tile("drawn", Rotate, Rotate, Bounce, Cards.Blank));

        Assert.That(game.RemainingUses(OrdinaryCatalog.Rotate), Is.EqualTo(2));
        Assert.That(game.RemainingUses(OrdinaryCatalog.Bounce), Is.EqualTo(1));

        var once = See.Game(RepresentativeDeck.Rotate(game, 0, 0));
        Assert.That(once.RemainingUses(OrdinaryCatalog.Rotate), Is.EqualTo(1));
        Assert.That(once.RemainingUses(OrdinaryCatalog.Bounce), Is.EqualTo(1));
        Assert.That(game.RemainingUses(OrdinaryCatalog.Rotate), Is.EqualTo(2), "the earlier match is unchanged");

        var twice = See.Game(RepresentativeDeck.Rotate(once, 0, 0));
        Assert.That(twice.RemainingUses(OrdinaryCatalog.Rotate), Is.EqualTo(0));
        Assert.That(RepresentativeDeck.Try(twice, new UseRotate(0, 0, 1)).Rejection?.Reason, Is.EqualTo(RejectionReason.NoUseRemaining));
    }

    [Test]
    public void RemainingUses_ResetForTheNextDrawnTile_AndAreZeroForOtherSymbols()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile("first", Rotate, Cards.Blank, Cards.Blank, Cards.Blank),
            Cards.Tile("second", Rotate, Rotate, Stack, Cards.Blank));

        var spent = See.Game(RepresentativeDeck.Rotate(game, 0, 0));
        Assert.That(spent.RemainingUses(OrdinaryCatalog.Rotate), Is.EqualTo(0));

        var next = See.Game(RepresentativeDeck.Play(spent, 1, 0));
        Assert.That(next.RemainingUses(OrdinaryCatalog.Rotate), Is.EqualTo(2));
        Assert.That(next.RemainingUses(OrdinaryCatalog.Stack), Is.EqualTo(0));
        Assert.That(next.RemainingUses(OrdinaryCatalog.Blank), Is.EqualTo(0));
        Assert.That(next.RemainingUses(new SymbolId("unlisted")), Is.EqualTo(0));
    }

    [Test]
    public void RemainingUses_AreZero_WhenNoTileIsPending()
    {
        var game = RepresentativeDeck.TwoSeats(Cards.BlankTile("start"));

        Assert.That(game.RemainingUses(OrdinaryCatalog.Rotate), Is.EqualTo(0));
        Assert.That(game.RemainingUses(OrdinaryCatalog.Bounce), Is.EqualTo(0));
    }

    [Test]
    public void Queries_ChangeNothing_AndAnswerTheSameTwice()
    {
        var game = SurroundedCenter(Rotate);

        var placements = game.LegalPlacements(1);
        var targets = game.LegalTargets(OrdinaryCatalog.Rotate);
        var remaining = game.RemainingUses(OrdinaryCatalog.Rotate);

        Assert.That(game.LegalPlacements(1), Is.EqualTo(placements));
        Assert.That(game.LegalTargets(OrdinaryCatalog.Rotate), Is.EqualTo(targets));
        Assert.That(game.RemainingUses(OrdinaryCatalog.Rotate), Is.EqualTo(remaining));
        Assert.That(game.TileCount, Is.EqualTo(5));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
    }

    // The start tile at the origin with a tile on each of its four sides, then the drawn tile showing one power.
    private static Game SurroundedCenter(Cell power)
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.Cross("start"),
            Cards.BlankTile("east"),
            Cards.BlankTile("west"),
            Cards.BlankTile("north"),
            Cards.BlankTile("south"),
            Cards.Tile("drawn", power, Cards.Blank, Cards.Blank, Cards.Blank));
        foreach (var (x, y) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            game = See.Game(RepresentativeDeck.Play(game, x, y));
        }

        return game;
    }
}
