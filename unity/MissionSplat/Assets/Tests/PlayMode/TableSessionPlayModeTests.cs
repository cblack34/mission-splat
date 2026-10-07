namespace MissionSplat.Player.Tests
{

using System.Linq;
using MissionSplat.App;
using MissionSplat.Player;
using MissionSplat.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.UI;

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

    [Test]
    public void InteractiveLayoutsSitInsideTheReportedSafeArea()
    {
        _root = new GameObject("Table");
        _root.AddComponent<TableSession>();

        var canvas = FindRect("Canvas");
        var safe = FindRect("SafeArea");
        var play = FindRect("Play");
        var setup = FindRect("Setup");
        var background = FindRect("Background");
        Assert.That(canvas, Is.Not.Null);
        Assert.That(safe, Is.Not.Null);
        Assert.That(play, Is.Not.Null);
        Assert.That(setup, Is.Not.Null);
        Assert.That(background, Is.Not.Null);
        Assert.That(play.transform.parent, Is.SameAs(safe.transform));
        Assert.That(setup.transform.parent, Is.SameAs(safe.transform));
        Assert.That(background.transform.parent, Is.SameAs(canvas.transform));

        var width = Screen.width;
        var height = Screen.height;
        var area = Screen.safeArea;
        if (width <= 0 || height <= 0 || area.width <= 0f || area.height <= 0f)
        {
            Assert.That(safe.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(safe.anchorMax, Is.EqualTo(Vector2.one));
            return;
        }

        var min = new Vector2(Mathf.Clamp01(area.xMin / width), Mathf.Clamp01(area.yMin / height));
        var max = new Vector2(Mathf.Clamp01(area.xMax / width), Mathf.Clamp01(area.yMax / height));
        if (max.x <= min.x || max.y <= min.y)
        {
            min = Vector2.zero;
            max = Vector2.one;
        }

        Assert.That(safe.anchorMin.x, Is.EqualTo(min.x).Within(0.0001f));
        Assert.That(safe.anchorMin.y, Is.EqualTo(min.y).Within(0.0001f));
        Assert.That(safe.anchorMax.x, Is.EqualTo(max.x).Within(0.0001f));
        Assert.That(safe.anchorMax.y, Is.EqualTo(max.y).Within(0.0001f));
        Assert.That(safe.offsetMin, Is.EqualTo(Vector2.zero));
        Assert.That(safe.offsetMax, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void RuntimeInputModule_BindsPointClickAndSubmit()
    {
        _root = new GameObject("Table");
        _root.AddComponent<TableSession>();

        var module = _root.GetComponentInChildren<InputSystemUIInputModule>(true);
        Assert.That(module, Is.Not.Null);
        Assert.That(module.point?.action, Is.Not.Null);
        Assert.That(module.leftClick?.action, Is.Not.Null);
        Assert.That(module.submit?.action, Is.Not.Null);
        Assert.That(module.point.action.bindings.Any(binding => binding.path.Contains("Touchscreen")), Is.True);
        Assert.That(module.leftClick.action.bindings.Any(binding => binding.path.Contains("Touchscreen")), Is.True);
    }

    private RectTransform FindRect(string name)
    {
        foreach (var rect in _root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == name)
            {
                return rect;
            }
        }

        return null;
    }
}
}
