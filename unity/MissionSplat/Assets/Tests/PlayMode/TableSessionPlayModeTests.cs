namespace MissionSplat.Player.Tests
{

using System.Linq;
using MissionSplat.App;
using MissionSplat.Player;
using MissionSplat.Rules;
using NUnit.Framework;
using UnityEngine;

public class TableSessionPlayModeTests
{
    private GameObject _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void ScriptedHumanPlacement_ThenOneAiTurn_PlacesThroughLocalSession()
    {
        Assert.That(TableDeck.Name, Is.EqualTo("table"));
        Assert.That(OrdinaryCatalog.ClaimsRequiredToWin, Is.EqualTo(4));

        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.HumanThenAi());

        Assert.That(table.IsStarted, Is.True);
        Assert.That(table.View.CurrentSeat.Value, Is.EqualTo("1"));
        Assert.That(table.View.Board.Tiles.Count, Is.EqualTo(1));
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Assert.That(table.Snapshot.SecretsVisible, Is.False);
        Assert.That(table.Highlights, Is.Empty);

        table.ConfirmIncomingSeat();

        Assert.That(table.Snapshot.ConcealVisible, Is.False);
        Assert.That(table.Snapshot.SecretsVisible, Is.True);
        Assert.That(table.View.UnclaimedMissions.Count, Is.EqualTo(2));
        Assert.That(table.Highlights, Is.Not.Empty);
        Assert.That(table.Highlights.Any(slot => slot.Equals(new Placement(1, 1, 0))), Is.False);
        Assert.That(table.Highlights.Any(slot => slot.Equals(new Placement(1, 0, 0))), Is.True);

        table.Tap(new Placement(1, 0, 0));

        Assert.That(table.View.Board.Tiles.Count, Is.EqualTo(3));
        Assert.That(table.View.CurrentSeat.Value, Is.EqualTo("1"));
        Assert.That(table.View.HasEnded, Is.False);
        Assert.That(table.View.Claims.Single(row => row.Seat.Value == "1").Missions.Count, Is.EqualTo(1));
        Assert.That(table.View.UnclaimedMissions.Count, Is.EqualTo(2));
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Assert.That(table.Snapshot.SecretsVisible, Is.False);
    }
}
}
