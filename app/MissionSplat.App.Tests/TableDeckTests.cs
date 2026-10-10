namespace MissionSplat.App.Tests;

using MissionSplat.App;
using MissionSplat.Rules;

public class TableDeckTests
{
    private static readonly SeatId[] Seats = [new SeatId("1"), new SeatId("2")];

    [Test]
    public void ANullSeed_KeepsTheAuthoredOrder()
    {
        var setup = TableDeck.Ordinary(Seats, Seats[0], null);

        Assert.That(setup.MatchDeck[0].Id.Value, Is.EqualTo("start"));
        Assert.That(setup.MatchDeck[1].Id.Value, Is.EqualTo("solid-red"));
        Assert.That(setup.MissionDeck[0].Id.Value, Is.EqualTo("square-red-1"));
        Assert.That(Ids(TableDeck.Ordinary(Seats, Seats[0], null)), Is.EqualTo(Ids(setup)));
        Assert.That(setup.MissionDeck, Has.Count.EqualTo(36));
        Assert.That(setup.MatchDeck, Has.Count.EqualTo(48));
    }

    [Test]
    public void TheSameSeed_DealsTheSameOrder_ADifferentSeedDoesNot()
    {
        var seeded = Ids(TableDeck.Ordinary(Seats, Seats[0], 11));

        Assert.That(Ids(TableDeck.Ordinary(Seats, Seats[0], 11)), Is.EqualTo(seeded));
        Assert.That(Ids(TableDeck.Ordinary(Seats, Seats[0], 12)), Is.Not.EqualTo(seeded));
        Assert.That(seeded, Is.Not.EqualTo(Ids(TableDeck.Ordinary(Seats, Seats[0], null))));
    }

    [Test]
    public void AShuffle_KeepsEveryTileAndMission()
    {
        var authored = TableDeck.Ordinary(Seats, Seats[0], null);
        var shuffled = TableDeck.Ordinary(Seats, Seats[0], 99);

        Assert.That(shuffled.MatchDeck.Select(tile => tile.Id.Value), Is.EquivalentTo(authored.MatchDeck.Select(tile => tile.Id.Value)));
        Assert.That(shuffled.MissionDeck.Select(mission => mission.Id.Value), Is.EquivalentTo(authored.MissionDeck.Select(mission => mission.Id.Value)));
        Assert.That(Game.Start(shuffled).IsAccepted, Is.True);
        Assert.That(TableDeck.Name, Is.EqualTo("table"));
    }

    private static string[] Ids(GameSetup setup) =>
        setup.MatchDeck.Select(tile => tile.Id.Value).Concat(setup.MissionDeck.Select(mission => mission.Id.Value)).ToArray();
}
