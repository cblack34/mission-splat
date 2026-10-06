namespace MissionSplat.Rules.Tests;

public class WinTests
{
    private static readonly ColorId C1 = new("c1");
    private static readonly ColorId C2 = new("c2");
    private static readonly ColorId C3 = new("c3");
    private static readonly ColorId C4 = new("c4");

    [Test]
    public void OrdinaryWinCount_DoesNotWinAtThree_WinsAtFour_AndRejectsTheNextCommand()
    {
        Assert.That(OrdinaryCatalog.ClaimsRequiredToWin, Is.EqualTo(4));
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Square("c1", C1),
                Square("c2", C2),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Square("c3", C3),
                Square("c4", C4),
                Cards.Purple("filler-1"),
                Cards.Purple("filler-2"),
                Cards.Purple("spare"),
            ],
            [
                Cards.BlankTile("start"),
                Cards.Solid("a1", Cell.Color(C1)),
                Cards.BlankTile("b1"),
                Cards.Solid("a2", Cell.Color(C2)),
                Cards.BlankTile("b2"),
                Cards.Solid("a3", Cell.Color(C3)),
                Cards.BlankTile("b3"),
                Cards.Solid("a4", Cell.Color(C4)),
                Cards.BlankTile("spare"),
            ],
            colors: [C1, C2, C3, C4, OrdinaryCatalog.Purple]);

        Assert.That(game.ClaimsRequiredToWin, Is.EqualTo(4));
        game = After(game, 0, 1);
        game = After(game, 0, -1);
        game = After(game, 0, 2);
        game = After(game, 0, -2);
        var third = RepresentativeDeck.Play(game, 0, 3);
        game = See.Game(third);
        Assert.That(game.Claims(Cards.Seat("a")), Has.Count.EqualTo(3));
        Assert.That(game.HasEnded, Is.False);
        Assert.That(third.Events.OfType<GameWon>(), Is.Empty);

        game = After(game, 0, -3);
        var fourth = RepresentativeDeck.Play(game, 0, 4);
        game = See.Game(fourth);
        Assert.That(game.Claims(Cards.Seat("a")), Has.Count.EqualTo(4));
        Assert.That(game.Hand(Cards.Seat("a")), Has.Count.EqualTo(2));
        Assert.That(game.HasEnded, Is.True);
        var won = fourth.Events.OfType<GameWon>().Single();
        Assert.That(won.Seat, Is.EqualTo(Cards.Seat("a")));
        Assert.That(won.ClaimCount, Is.EqualTo(4));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("b")));

        var tiles = game.TileCount;
        var remaining = game.MatchDeckRemaining;
        var rejected = game.Place(1, 0, 0);
        Assert.That(rejected.IsAccepted, Is.False);
        Assert.That(rejected.Rejection?.Reason, Is.EqualTo(RejectionReason.GameOver));
        Assert.That(rejected.Events, Is.Empty);
        Assert.That(rejected.Game, Is.SameAs(game));
        Assert.That(game.TileCount, Is.EqualTo(tiles));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(remaining));
        Assert.That(remaining, Is.GreaterThan(0));
    }

    [Test]
    public void TwoClaimsThatCrossTheWinCount_BothCount_AndThePlacementIsAccepted()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Square("c1", C1),
                Square("c2", C2),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Square("c3", C3),
                Cards.Mission("red-row", MissionPattern.Row, OrdinaryCatalog.Red),
                Cards.Mission("red-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Purple("filler-1"),
                Cards.Purple("filler-2"),
                Cards.Purple("spare"),
            ],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Solid("a1", Cell.Color(C1)),
                Cards.BlankTile("b1"),
                Cards.Solid("a2", Cell.Color(C2)),
                Cards.BlankTile("b2"),
                Cards.Solid("a3", Cell.Color(C3)),
                Cards.BlankTile("b3"),
                Cards.Solid("reds", Cards.Red),
                Cards.BlankTile("spare"),
            ],
            colors: [C1, C2, C3, OrdinaryCatalog.Red, OrdinaryCatalog.Purple]);

        game = After(game, 0, 1);
        game = After(game, 0, -1);
        game = After(game, 0, 2);
        game = After(game, 0, -2);
        var third = RepresentativeDeck.Play(game, 0, 3);
        game = See.Game(third);
        Assert.That(See.Ids(game.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row", "red-square" }));
        Assert.That(game.Claims(Cards.Seat("a")), Has.Count.EqualTo(3));
        Assert.That(game.HasEnded, Is.False);

        game = After(game, 0, -3);
        var crossing = RepresentativeDeck.Play(game, 1, 0);
        game = See.Game(crossing);
        Assert.That(crossing.IsAccepted, Is.True);
        Assert.That(See.Ids(game.Claims(Cards.Seat("a"))), Is.EqualTo(new[]
        {
            "c1", "c2", "c3", "red-row", "red-square",
        }));
        Assert.That(game.Hand(Cards.Seat("a")), Has.Count.EqualTo(2));
        Assert.That(game.HasEnded, Is.True);
        Assert.That(crossing.Events.OfType<GameWon>().Single().ClaimCount, Is.EqualTo(5));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("b")));

        var rejected = game.Place(2, 0, 0);
        Assert.That(rejected.Rejection?.Reason, Is.EqualTo(RejectionReason.GameOver));
        Assert.That(rejected.Game, Is.SameAs(game));
    }

    [Test]
    public void SmallerWinCount_WinsAtThatCount()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.Mission("red-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Mission("blue-square", MissionPattern.Square, OrdinaryCatalog.Blue),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("filler-1"),
                Cards.Purple("filler-2"),
            ],
            [
                Cards.BlankTile("start"),
                Cards.Solid("red", Cards.Red),
                Cards.BlankTile("gap"),
                Cards.Solid("blue", Cards.Blue),
                Cards.BlankTile("spare"),
            ],
            claimsRequiredToWin: 2);

        Assert.That(game.ClaimsRequiredToWin, Is.EqualTo(2));
        var first = RepresentativeDeck.Play(game, 1, 0);
        game = See.Game(first);
        Assert.That(game.Claims(Cards.Seat("a")), Has.Count.EqualTo(1));
        Assert.That(game.HasEnded, Is.False);
        Assert.That(first.Events.OfType<GameWon>(), Is.Empty);

        game = After(game, 2, 0);
        var second = RepresentativeDeck.Play(game, 1, 1);
        game = See.Game(second);
        Assert.That(game.Claims(Cards.Seat("a")), Has.Count.EqualTo(2));
        Assert.That(game.HasEnded, Is.True);
        Assert.That(second.Events.OfType<GameWon>().Single().ClaimCount, Is.EqualTo(2));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("b")));

        var rejected = game.Place(0, 1, 0);
        Assert.That(rejected.Rejection?.Reason, Is.EqualTo(RejectionReason.GameOver));
        Assert.That(rejected.Game, Is.SameAs(game));
    }

    private static Mission Square(string id, ColorId color) =>
        Cards.Mission(id, MissionPattern.Square, color);

    private static Game After(Game game, int tileX, int tileY) =>
        See.Game(RepresentativeDeck.Play(game, tileX, tileY));
}
