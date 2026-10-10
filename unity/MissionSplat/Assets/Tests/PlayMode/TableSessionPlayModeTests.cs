namespace MissionSplat.Player.Tests
{

using System.Linq;
using MissionSplat.App;
using MissionSplat.Player;
using MissionSplat.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

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
        Assert.That(OrdinaryCatalog.ClaimsRequiredToWin, Is.EqualTo(4));

        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.HumanThenAi());

        Assert.That(table.IsStarted, Is.True);
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("1"));
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Assert.That(table.Snapshot.SecretsVisible, Is.False);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToConfirm));
        Assert.That(table.Snapshot.LegalPlacements, Is.Empty);

        table.ConfirmIncomingSeat();

        var placements = table.Snapshot.LegalPlacements;
        Assert.That(table.Snapshot.ConcealVisible, Is.False);
        Assert.That(table.Snapshot.SecretsVisible, Is.True);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
        Assert.That(table.Snapshot.View.UnclaimedMissions.Count, Is.EqualTo(2));
        Assert.That(placements, Is.Not.Empty);
        Assert.That(placements.Any(slot => slot.TileX == 1 && slot.TileY == 1), Is.False);
        Assert.That(placements.Any(slot => slot.TileX == 1 && slot.TileY == 0), Is.True);

        table.Tap(1, 0);

        var view = table.Snapshot.View;
        Assert.That(view.Board.Tiles.Count, Is.EqualTo(3));
        Assert.That(view.CurrentSeat.Value, Is.EqualTo("1"));
        Assert.That(view.HasEnded, Is.False);
        Assert.That(view.Claims.Single(row => row.Seat.Value == "1").Missions.Count, Is.EqualTo(1));
        Assert.That(view.UnclaimedMissions.Count, Is.EqualTo(2));
        // The same human holds the device after an AI turn, so the hand stays visible.
        Assert.That(table.Snapshot.ConcealVisible, Is.False);
        Assert.That(table.Snapshot.SecretsVisible, Is.True);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
    }

    [Test]
    public void StackTile_OffersAnOnTopPlacement_AndTappingItCoversWithoutAddingATile()
    {
        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.PassAndPlayDraft(), StackSetup());
        table.ConfirmIncomingSeat();

        var before = table.Snapshot.View.Board;
        var onTop = table.Snapshot.LegalPlacements.Where(slot => slot.Kind == PlacementKind.OnTop).ToArray();
        Assert.That(onTop, Is.EqualTo(new[] { new LegalPlacement(0, 0, PlacementKind.OnTop) }));
        Assert.That(table.Snapshot.LegalPlacements.Any(slot => slot.Kind == PlacementKind.Beside), Is.True);
        Assert.That(before.Tiles.Count, Is.EqualTo(1));
        Assert.That(before.Cells.All(cell => cell.Value.Equals(Cell.Symbol(OrdinaryCatalog.Blank))), Is.True);

        table.Tap(onTop[0].TileX, onTop[0].TileY);

        var after = table.Snapshot.View.Board;
        Assert.That(after.Tiles.Count, Is.EqualTo(1));
        Assert.That(after.Tiles[0].Id.Value, Is.EqualTo("lid"));
        Assert.That(after.Cells.Any(cell => cell.Value.Equals(Cell.Color(OrdinaryCatalog.Red))), Is.True);
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
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
    public void StatusStopTextWrapsInsideItsRect()
    {
        _root = new GameObject("Table");
        _root.AddComponent<TableSession>();

        var status = FindText("Status");
        var confirm = FindRect("Confirm");
        Assert.That(status, Is.Not.Null);
        Assert.That(confirm, Is.Not.Null);
        Assert.That(status.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
        Assert.That(confirm.GetComponentInChildren<Text>(true).horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Overflow));

        status.transform.parent.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Assert.That(status.rectTransform.rect.height, Is.GreaterThan(status.fontSize));
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

    private static GameSetup StackSetup()
    {
        var blank = Cell.Symbol(OrdinaryCatalog.Blank);
        var red = Cell.Color(OrdinaryCatalog.Red);
        var blue = Cell.Color(OrdinaryCatalog.Blue);
        var stack = Cell.Symbol(OrdinaryCatalog.Stack);
        var missions = new[] { "a1", "a2", "b1", "b2", "spare" }
            .Select(id => new Mission(new MissionId(id), MissionPattern.Row, OrdinaryCatalog.Purple))
            .ToArray();
        var tiles = new[]
        {
            new Tile(new TileId("start"), blank, blank, blank, blank),
            new Tile(new TileId("lid"), stack, red, blue, blank),
            new Tile(new TileId("pad"), blank, blank, blank, blank),
        };
        var seats = TableStart.PassAndPlayDraft().SeatsInTurnOrder();
        return new GameSetup(
            seats,
            seats[0],
            OrdinaryCatalog.Colors,
            OrdinaryCatalog.NonScoringSymbols,
            OrdinaryCatalog.Patterns,
            OrdinaryCatalog.ClaimsRequiredToWin,
            missions,
            tiles);
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

    private Text FindText(string name)
    {
        foreach (var text in _root.GetComponentsInChildren<Text>(true))
        {
            if (text.name == name)
            {
                return text;
            }
        }

        return null;
    }
}
}
