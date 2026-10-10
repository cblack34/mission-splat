namespace MissionSplat.App.Tests;

using System.Reflection;
using MissionSplat.App;
using MissionSplat.Rules;

public class AiPlayerTests
{
    [Test]
    public void ChooseAction_OnTheClaimingTable_ClaimsOwnMission_AndHidesOtherHands()
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));
        var session = ClaimingTable.Open(3).Session;
        var seat = new SeatId("a");
        var ai = Ai(session, seat);
        var view = session.View(ai.Seat);

        Assert.That(Ids(view.UnclaimedMissions), Is.EqualTo(new[] { "a-square", "a-row" }));
        var visible = MissionIds.In(view);
        Assert.That(visible, Is.EquivalentTo(new[] { "a-square", "a-row" }));
        Assert.That(visible, Does.Not.Contain("b-square"));
        Assert.That(visible, Does.Not.Contain("b-row"));
        Assert.That(visible, Does.Not.Contain("c-square"));
        Assert.That(visible, Does.Not.Contain("c-row"));

        var chosen = ai.ChooseAction(view);

        Assert.That(chosen, Is.EqualTo(new Place(-1, 0, 0)));
        var preview = session.Preview(seat, chosen);
        Assert.That(preview.IsAccepted, Is.True, preview.Rejection?.Message);
        Assert.That(preview.ClaimedMissions.Select(mission => mission.Value), Is.EqualTo(new[] { "a-square" }));

        var placed = session.Submit(seat, chosen);
        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
        Assert.That(
            placed.Events.OfType<MissionClaimed>().Select(claim => claim.Mission.Value),
            Is.EqualTo(new[] { "a-square" }));
    }

    [Test]
    public void ChooseAction_PrefersALaterOwnClaim_OverAnEarlierLegalPlacement()
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
        var earlier = session.Preview(seat, new Place(-1, 0, 0));
        Assert.That(earlier.IsAccepted, Is.True, earlier.Rejection?.Message);
        Assert.That(earlier.ClaimedMissions, Is.Empty);

        var ai = Ai(session, seat);
        var chosen = ai.ChooseAction(session.View(ai.Seat));

        Assert.That(chosen, Is.EqualTo(new Place(1, 0, 0)));
        var preview = session.Preview(seat, chosen);
        Assert.That(preview.IsAccepted, Is.True, preview.Rejection?.Message);
        Assert.That(preview.ClaimedMissions.Select(mission => mission.Value), Is.EqualTo(new[] { "a-square" }));
        var placed = session.Submit(seat, chosen);
        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);
    }

    [Test]
    public void ChooseAction_WhenNothingClaims_ReturnsTheFirstAcceptedPlacement()
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

        var ai = Ai(session, seat);
        var chosen = ai.ChooseAction(view);

        Assert.That(chosen, Is.EqualTo(new Place(-1, 0, 0)));
        var preview = session.Preview(seat, chosen);
        Assert.That(preview.IsAccepted, Is.True, preview.Rejection?.Message);
        Assert.That(preview.ClaimedMissions, Is.Empty);
        var placed = session.Submit(seat, chosen);
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

        var choose = typeof(AiPlayer).GetMethod(nameof(AiPlayer.ChooseAction), flags);
        Assert.That(choose, Is.Not.Null);
        foreach (var parameter in choose!.GetParameters())
        {
            AssertNoneOf(parameter.ParameterType, forbidden, parameter.Name ?? "parameter");
        }
    }

    [Test]
    public void ChooseAction_ChoosesOnlyFromTheSessionsLegalPlacements()
    {
        var asked = new List<int>();
        var ai = new AiPlayer(
            new SeatId("a"),
            quarterTurns =>
            {
                asked.Add(quarterTurns);
                return quarterTurns == 2 ? [new LegalPlacement(5, 5, PlacementKind.Beside)] : [];
            },
            _ => ActionPreview.Accept([]));

        var chosen = ai.ChooseAction(ClaimingTable.Open(3).Session.View(new SeatId("a")));

        Assert.That(chosen, Is.EqualTo(new Place(5, 5, 2)));
        Assert.That(asked, Is.EqualTo(new[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void ChooseAction_WithNoLegalPlacement_ThrowsNamingTheDrawnTile()
    {
        var session = ClaimingTable.Open(3).Session;
        var ai = new AiPlayer(new SeatId("a"), _ => [], _ => ActionPreview.Accept([]));

        Assert.That(
            () => ai.ChooseAction(session.View(new SeatId("a"))),
            Throws.InvalidOperationException.With.Message.EqualTo("No accepted placement was found for tile 'a-tile'."));
    }

    [Test]
    public void ChooseAction_WithAPowerTileDrawn_StillPlacesBeside()
    {
        var session = Open(
            [Cards.Row("a1"), Cards.Row("a2"), Cards.Row("b1"), Cards.Row("b2"), Cards.Row("spare")],
            [
                Cards.BlankTile("start"),
                Cards.Tile("lid", Cell.Symbol(OrdinaryCatalog.Stack), Cards.Blank, Cards.Blank, Cards.Blank),
            ]);
        var seat = new SeatId("a");
        Assert.That(session.LegalPlacements(seat, 0), Has.Some.Matches<LegalPlacement>(legal => legal.Kind == PlacementKind.OnTop));

        var chosen = Ai(session, seat).ChooseAction(session.View(seat));

        Assert.That(chosen, Is.EqualTo(new Place(-1, 0, 0)));
    }

    private static AiPlayer Ai(LocalSession session, SeatId seat) =>
        new(seat, quarterTurns => session.LegalPlacements(seat, quarterTurns), action => session.Preview(seat, action));

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
}
