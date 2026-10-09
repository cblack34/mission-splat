namespace MissionSplat.Rules.Tests;

public class ResolutionTests
{
    [Test]
    public void OneTile_ClaimsBothMissionsTheActingSeatHolds_AndPassesOnce()
    {
        var placed = RepresentativeDeck.Play(RowAndSquareGame(), 1, 0);
        var next = See.Game(placed);

        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row", "red-square" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "repl-1", "repl-2" }));
        Assert.That(next.Hand(Cards.Seat("a")), Has.Count.EqualTo(2));
        Assert.That(next.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
        Assert.That(placed.Events.OfType<TilePlaced>().Count(), Is.EqualTo(1));
        Assert.That(
            placed.Events.OfType<MissionClaimed>().Select(claim => claim.Mission.Value).ToArray(),
            Is.EqualTo(new[] { "red-row", "red-square" }));
        Assert.That(placed.Events.OfType<GameWon>(), Is.Empty);
        Assert.That(next.HasEnded, Is.False);
    }

    [Test]
    public void ReplacementDrawnDuringResolution_IsNotClaimedEvenWhenTheBoardShowsIt()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Mission("red-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Purple("spare"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Solid("finish", Cards.Red),
            ]);

        var placed = RepresentativeDeck.Play(game, 1, 0);
        var next = See.Game(placed);

        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "a2", "red-square" }));
        Assert.That(placed.Events.OfType<MissionClaimed>().Count(), Is.EqualTo(1));
        Assert.That(next.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
    }

    [TestCase(2)]
    [TestCase(3)]
    public void StolenPattern_IsNotClaimed_EvenAfterALaterTileMissesIt(int seatCount)
    {
        var names = new[] { "a", "b", "c" }.Take(seatCount).ToArray();
        var missions = new List<Mission>
        {
            Cards.Purple("a1"),
            Cards.Purple("a2"),
            Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
            Cards.Purple("b2"),
        };
        for (var i = 2; i < seatCount; i++)
        {
            missions.Add(Cards.Purple(names[i] + "1"));
            missions.Add(Cards.Purple(names[i] + "2"));
        }

        missions.Add(Cards.Purple("spare"));
        var game = RepresentativeDeck.Start(
            names,
            "a",
            missions.ToArray(),
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("stolen", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.BlankTile("miss"),
            ]);

        Assert.That(See.Ids(game.Hand(Cards.Seat("b"))), Does.Contain("red-row"));

        var stolen = See.Game(RepresentativeDeck.Play(game, 1, 0));
        Assert.That(stolen.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(stolen.CellAt(3, 0), Is.EqualTo(Cards.Red));
        AssertNoClaims(stolen, names);

        var missed = See.Game(RepresentativeDeck.Play(stolen, 2, 0));
        Assert.That(missed.CellAt(4, 0), Is.EqualTo(Cards.Blank));
        Assert.That(missed.CellAt(5, 0), Is.EqualTo(Cards.Blank));
        AssertNoClaims(missed, names);
        Assert.That(See.Ids(missed.Hand(Cards.Seat("b"))), Does.Contain("red-row"));
    }

    [Test]
    public void EmptyMissionDeck_AcceptsABlankFullSide_AndClaimsNothing()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Purple("a1"),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
            ],
            [
                Cards.BlankTile("start"),
                Cards.BlankTile("beside"),
            ]);

        Assert.That(game.MissionDeckRemaining, Is.EqualTo(0));

        var placed = RepresentativeDeck.Play(game, 1, 0);
        var next = See.Game(placed);

        Assert.That(next.TileCount, Is.EqualTo(2));
        Assert.That(next.MissionDeckRemaining, Is.EqualTo(0));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(next.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "a1", "a2" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("b"))), Is.EqualTo(new[] { "b1", "b2" }));
        Assert.That(placed.Events.OfType<MissionClaimed>(), Is.Empty);
    }

    [Test]
    public void EmptyMissionDeck_ThrowsWhenThePlacementWouldClaimOne_AndTheGameStays()
    {
        // Same full-side row as HorizontalRow_ClaimsForTheFinisher, with no replacement left.
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("finish", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
            ]);

        Assert.That(game.MissionDeckRemaining, Is.EqualTo(0));
        AssertMissionShortfallUnchanged(game);
        AssertCornerStillRejected(game);
    }

    [Test]
    public void OneReplacement_ThrowsWhenThePlacementWouldClaimTwo_AndTheGameStays()
    {
        // Same both-missions placement as OneTile_ClaimsBothMissions, with one replacement left.
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Mission("red-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("repl-1"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Solid("finish", Cards.Red),
            ]);

        Assert.That(game.MissionDeckRemaining, Is.EqualTo(1));
        AssertMissionShortfallUnchanged(game);
        AssertCornerStillRejected(game);
    }

    private static Game RowAndSquareGame() =>
        RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Mission("red-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("repl-1"),
                Cards.Purple("repl-2"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Solid("finish", Cards.Red),
            ]);

    private static void AssertNoClaims(Game game, IReadOnlyList<string> names)
    {
        foreach (var name in names)
        {
            Assert.That(game.Claims(Cards.Seat(name)), Is.Empty);
        }
    }

    private static void AssertMissionShortfallUnchanged(Game game)
    {
        var tiles = game.TileCount;
        var matchRemaining = game.MatchDeckRemaining;
        var seat = game.CurrentSeat;
        var handA = See.Ids(game.Hand(Cards.Seat("a")));
        var handB = See.Ids(game.Hand(Cards.Seat("b")));
        var claimsA = See.Ids(game.Claims(Cards.Seat("a")));
        var claimsB = See.Ids(game.Claims(Cards.Seat("b")));

        Assert.That(
            () => { RepresentativeDeck.Try(game, new Place(1, 0, 0)); },
            Throws.TypeOf<UnresolvedRulingException>().With.Message.EqualTo(
                "The mission deck cannot replace every mission this placement completed. Exhausting the mission deck is an open ruling, so this command was not applied."));

        Assert.That(game.TileCount, Is.EqualTo(tiles));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(matchRemaining));
        Assert.That(game.CurrentSeat, Is.EqualTo(seat));
        Assert.That(See.Ids(game.Hand(Cards.Seat("a"))), Is.EqualTo(handA));
        Assert.That(See.Ids(game.Hand(Cards.Seat("b"))), Is.EqualTo(handB));
        Assert.That(See.Ids(game.Claims(Cards.Seat("a"))), Is.EqualTo(claimsA));
        Assert.That(See.Ids(game.Claims(Cards.Seat("b"))), Is.EqualTo(claimsB));
    }

    private static void AssertCornerStillRejected(Game game)
    {
        var corner = RepresentativeDeck.Try(game, new Place(1, 1, 0));

        Assert.That(corner.IsAccepted, Is.False);
        Assert.That(corner.Rejection?.Reason, Is.EqualTo(RejectionReason.DoesNotShareFullSide));
        Assert.That(corner.Events, Is.Empty);
        Assert.That(corner.Game, Is.SameAs(game));
    }
}
