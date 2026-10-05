namespace MissionSplat.Rules.Tests;

public class SetupTests
{
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void Setup_DealsTwoDistinctMissions_AndDoesNotClaim(int seatCount)
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));

        var names = new[] { "a", "b", "c", "d" }.Take(seatCount).ToArray();
        var first = names[^1];
        var missions = new Mission[seatCount * 2 + 1];
        missions[0] = Cards.Mission("square-red", MissionPattern.Square, OrdinaryCatalog.Red);
        for (var i = 1; i < missions.Length; i++)
        {
            missions[i] = Cards.Purple("m" + i);
        }

        var result = RepresentativeDeck.Open(
            names,
            first,
            missions,
            [Cards.Solid("start", Cards.Red), Cards.BlankTile("later")]);

        Assert.That(result.IsAccepted, Is.True, "representative deck: " + result.Rejection?.Message);
        Assert.That(result.Events, Is.Empty);
        var game = See.Game(result);

        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat(first)));
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.HasTileAt(0, 0), Is.True);
        Assert.That(game.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(game.CellAt(1, 1), Is.EqualTo(Cards.Red));
        Assert.That(game.ClaimsRequiredToWin, Is.EqualTo(OrdinaryCatalog.ClaimsRequiredToWin));
        Assert.That(game.ClaimsRequiredToWin, Is.EqualTo(4));
        Assert.That(game.MatchDeckRemaining, Is.EqualTo(1));
        Assert.That(game.MissionDeckRemaining, Is.EqualTo(1));

        var dealt = new List<string>();
        for (var i = 0; i < seatCount; i++)
        {
            var seat = Cards.Seat(names[i]);
            var hand = game.Hand(seat);
            Assert.That(hand, Has.Count.EqualTo(2));
            Assert.That(See.Ids(hand), Is.EqualTo(new[] { missions[i * 2].Id.Value, missions[(i * 2) + 1].Id.Value }));
            Assert.That(game.Claims(seat), Is.Empty);
            dealt.AddRange(See.Ids(hand));
        }

        Assert.That(dealt, Is.Unique);
        Assert.That(dealt, Does.Contain("square-red"));
    }

    [Test]
    public void StartingWildcard_StaysWild_AndSetupDoesNotClaim()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "b",
            [Cards.Purple("a1"), Cards.Purple("a2"), Cards.Purple("b1"), Cards.Purple("b2")],
            [Cards.Tile("start", Cards.Wild, Cards.Blank, Cards.Blank, Cards.Blank), Cards.BlankTile("later")]);

        var wild = game.CellAt(0, 0);
        Assert.That(wild?.IsWild, Is.True);
        Assert.That(wild?.TryGetColor(out _), Is.False);
        Assert.That(wild?.CountsAs(OrdinaryCatalog.Red), Is.True);
        Assert.That(wild?.CountsAs(OrdinaryCatalog.Blue), Is.True);
        Assert.That(game.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(game.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(game.TileCount, Is.EqualTo(1));
        Assert.That(game.CurrentSeat, Is.EqualTo(Cards.Seat("b")));
    }
}
