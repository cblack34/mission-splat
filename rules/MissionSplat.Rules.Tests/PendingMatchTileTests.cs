namespace MissionSplat.Rules.Tests;

public class PendingMatchTileTests
{
    [Test]
    public void StartedGame_ExposesTheMatchTileAfterTheStartingTile()
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));

        var next = Cards.Tile("next", Cards.Red, Cards.Blue, Cards.Wild, Cards.Blank);
        var later = Cards.Tile("later", Cards.Blank, Cards.Red, Cards.Blue, Cards.Wild);
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
            [Cards.BlankTile("start"), next, later]);

        var pending = game.PendingMatchTile;
        Assert.That(pending, Is.Not.Null);
        Assert.That(pending!.Id, Is.EqualTo(next.Id));
        Assert.That(pending.Local(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(pending.Local(1, 0), Is.EqualTo(Cards.Blue));
        Assert.That(pending.Local(0, 1), Is.EqualTo(Cards.Wild));
        Assert.That(pending.Local(1, 1), Is.EqualTo(Cards.Blank));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(2));

        var after = See.Game(RepresentativeDeck.Play(game, 1, 0));
        var advanced = after.PendingMatchTile;

        Assert.That(advanced, Is.Not.Null);
        Assert.That(advanced!.Id, Is.EqualTo(later.Id));
        Assert.That(advanced.Local(0, 0), Is.EqualTo(Cards.Blank));
        Assert.That(advanced.Local(1, 0), Is.EqualTo(Cards.Red));
        Assert.That(advanced.Local(0, 1), Is.EqualTo(Cards.Blue));
        Assert.That(advanced.Local(1, 1), Is.EqualTo(Cards.Wild));
        Assert.That(after.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.PendingMatchTile!.Id, Is.EqualTo(next.Id));
    }

    [Test]
    public void EmptyRemainingDeck_ReportsNoPendingTile()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2"), Cards.Purple("spare")],
            [Cards.BlankTile("start")]);

        Assert.That(game.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(game.PendingMatchTile, Is.Null);
    }

    [Test]
    public void EndedGame_WithTilesRemaining_ReportsNoPendingTile()
    {
        var next = Cards.Solid("next", Cards.Red);
        var later = Cards.BlankTile("later");
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
            [Cards.BlankTile("start"), next, later],
            claimsRequiredToWin: 1);

        Assert.That(game.HasEnded, Is.False);
        Assert.That(game.PendingMatchTile, Is.Not.Null);
        Assert.That(game.PendingMatchTile!.Id, Is.EqualTo(next.Id));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(2));

        var won = See.Game(RepresentativeDeck.Play(game, 1, 0));

        Assert.That(won.HasEnded, Is.True);
        Assert.That(won.Claims(Cards.Seat("a")), Has.Count.EqualTo(1));
        Assert.That(won.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(won.PendingMatchTile, Is.Null);
        Assert.That(game.HasEnded, Is.False);
        Assert.That(game.PendingMatchTile!.Id, Is.EqualTo(next.Id));
    }
}
