namespace MissionSplat.App.Tests;

using System.Collections;
using System.Reflection;
using MissionSplat.App;
using MissionSplat.Rules;

public class AiPlayerTests
{
    [Test]
    public void ChoosePlacement_OnTheClaimingTable_ClaimsOwnMission_AndHidesOtherHands()
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));
        var session = ClaimingTable.Open(3).Session;
        var seat = new SeatId("a");
        var ai = new AiPlayer(seat, placement => session.Preview(seat, placement));
        var view = session.View(ai.Seat);

        Assert.That(Ids(view.UnclaimedMissions), Is.EqualTo(new[] { "a-square", "a-row" }));
        var visible = MissionIdsIn(view);
        Assert.That(visible, Is.EquivalentTo(new[] { "a-square", "a-row" }));
        Assert.That(visible, Does.Not.Contain("b-square"));
        Assert.That(visible, Does.Not.Contain("b-row"));
        Assert.That(visible, Does.Not.Contain("c-square"));
        Assert.That(visible, Does.Not.Contain("c-row"));

        var chosen = ai.ChoosePlacement(view);

        Assert.That(chosen, Is.EqualTo(new Placement(-1, 0, 0)));
        var preview = session.Preview(seat, chosen);
        Assert.That(preview.IsAccepted, Is.True, preview.Rejection?.Message);
        Assert.That(preview.ClaimedMissions.Select(mission => mission.Value), Is.EqualTo(new[] { "a-square" }));

        var placed = session.Place(seat, chosen);
        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
        Assert.That(
            placed.Events.OfType<MissionClaimed>().Select(claim => claim.Mission.Value),
            Is.EqualTo(new[] { "a-square" }));
    }

    [Test]
    public void ChoosePlacement_PrefersALaterOwnClaim_OverAnEarlierLegalPlacement()
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));
        // Start paints (1,0) and (1,1) red. Only (1, 0) quarter-turn 0 also paints (2,0) and (2,1).
        var session = Open(
            [
                Cards.Mission("a-square", MissionPattern.Square, OrdinaryCatalog.Red),
                Cards.Row("a-row"),
                Cards.Row("b-row"),
                Cards.Row("b-other"),
                Cards.Row("a-replacement"),
                Cards.Row("spare-mission"),
            ],
            [
                new Tile(new TileId("start"), Cards.Blank, Cards.Red, Cards.Blank, Cards.Red),
                new Tile(new TileId("edge"), Cards.Red, Cards.Blank, Cards.Red, Cards.Blank),
            ]);
        var seat = new SeatId("a");
        var earlier = session.Preview(seat, new Placement(-1, 0, 0));
        Assert.That(earlier.IsAccepted, Is.True, earlier.Rejection?.Message);
        Assert.That(earlier.ClaimedMissions, Is.Empty);

        var ai = new AiPlayer(seat, placement => session.Preview(seat, placement));
        var chosen = ai.ChoosePlacement(session.View(ai.Seat));

        Assert.That(chosen, Is.EqualTo(new Placement(1, 0, 0)));
        var preview = session.Preview(seat, chosen);
        Assert.That(preview.IsAccepted, Is.True, preview.Rejection?.Message);
        Assert.That(preview.ClaimedMissions.Select(mission => mission.Value), Is.EqualTo(new[] { "a-square" }));
        var placed = session.Place(seat, chosen);
        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
    }

    [Test]
    public void ChoosePlacement_WhenNothingClaims_ReturnsTheFirstAcceptedPlacement()
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));
        var session = Open(
            [
                Cards.Row("a-row"),
                Cards.Row("a-other"),
                Cards.Row("b-row"),
                Cards.Row("b-other"),
            ],
            [
                Cards.BlankTile("start"),
                Cards.BlankTile("pending"),
            ]);
        var seat = new SeatId("a");
        var view = session.View(seat);
        Assert.That(view.Board.Tiles, Has.Count.EqualTo(1));
        Assert.That(view.PendingMatchTile!.Id.Value, Is.EqualTo("pending"));

        var ai = new AiPlayer(seat, placement => session.Preview(seat, placement));
        var chosen = ai.ChoosePlacement(view);

        Assert.That(chosen, Is.EqualTo(new Placement(-1, 0, 0)));
        var preview = session.Preview(seat, chosen);
        Assert.That(preview.IsAccepted, Is.True, preview.Rejection?.Message);
        Assert.That(preview.ClaimedMissions, Is.Empty);
        var placed = session.Place(seat, chosen);
        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
    }

    [Test]
    public void AiPlayer_DoesNotReferenceTheSessionOrTheGame()
    {
        var forbidden = new[] { typeof(Game), typeof(ISession), typeof(LocalSession) };
        const BindingFlags flags = BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly;

        foreach (var field in typeof(AiPlayer).GetFields(flags))
        {
            AssertNoneOf(field.FieldType, forbidden, field.Name);
        }

        foreach (var constructor in typeof(AiPlayer).GetConstructors(flags))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                AssertNoneOf(parameter.ParameterType, forbidden, parameter.Name ?? "constructor");
            }
        }

        var choose = typeof(AiPlayer).GetMethod(nameof(AiPlayer.ChoosePlacement), flags);
        Assert.That(choose, Is.Not.Null);
        foreach (var parameter in choose!.GetParameters())
        {
            AssertNoneOf(parameter.ParameterType, forbidden, parameter.Name ?? "parameter");
        }
    }

    private static LocalSession Open(Mission[] missions, Tile[] tiles)
    {
        var session = new LocalSession();
        var started = session.Start(RepresentativeDeck.Setup(["a", "b"], "a", missions, tiles));
        Assert.That(started.IsAccepted, Is.True, RepresentativeDeck.Label + ": " + started.Rejection?.Message);
        return session;
    }

    private static string[] Ids(IReadOnlyList<Mission> missions) =>
        missions.Select(mission => mission.Id.Value).ToArray();

    private static void AssertNoneOf(Type type, Type[] forbidden, string site)
    {
        Assert.That(forbidden, Does.Not.Contain(type), site);
        if (type.IsArray && type.GetElementType() is Type element)
        {
            AssertNoneOf(element, forbidden, site);
        }

        if (!type.IsGenericType)
        {
            return;
        }

        foreach (var argument in type.GetGenericArguments())
        {
            AssertNoneOf(argument, forbidden, site);
        }
    }

    private static HashSet<string> MissionIdsIn(object root)
    {
        var ids = new HashSet<string>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Walk(root, ids, seen);
        return ids;
    }

    private static void Walk(object? value, ISet<string> ids, ISet<object> seen)
    {
        if (value is null)
        {
            return;
        }

        switch (value)
        {
            case MissionId mission:
                ids.Add(mission.Value);
                return;
            case Mission mission:
                ids.Add(mission.Id.Value);
                return;
            case string text:
                ids.Add(text);
                return;
            case Cell:
            case SeatId:
            case TileId:
            case ColorId:
            case SymbolId:
                return;
        }

        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum)
        {
            return;
        }

        if (value is IEnumerable enumerable)
        {
            if (!seen.Add(value))
            {
                return;
            }

            foreach (var item in enumerable)
            {
                Walk(item, ids, seen);
            }

            return;
        }

        if (type.IsClass && !seen.Add(value))
        {
            return;
        }

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            Walk(field.GetValue(value), ids, seen);
        }
    }
}
