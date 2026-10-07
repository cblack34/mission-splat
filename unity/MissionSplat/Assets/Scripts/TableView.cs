namespace MissionSplat.Player
{

using System;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class TableView : MonoBehaviour
{
    private RectTransform _root;
    private RectTransform _play;
    private RectTransform _setup;
    private Text _status;
    private RectTransform _pending;
    private RectTransform _secrets;
    private RectTransform _board;
    private RectTransform _claims;
    private RectTransform _rotation;
    private RectTransform _conceal;
    private Text _concealLabel;
    private TableStart _draft;

    public event Action<Placement> Tapped;
    public event Action Confirmed;
    public event Action<int> QuarterTurnsSelected;
    public event Action<TableStart> Started;

    private void Awake()
    {
        PaintCamera();
        BuildCanvas();
        ShowSetup(TableStart.PassAndPlayDraft());
    }

    private static void PaintCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = SplatPalette.Cream;
    }

    public void ShowSetup(TableStart draft)
    {
        _draft = draft;
        _play.gameObject.SetActive(false);
        _setup.gameObject.SetActive(true);
        RebuildSetup();
    }

    public void Render(TableSnapshot snapshot)
    {
        _setup.gameObject.SetActive(false);
        _play.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        _status.text = string.IsNullOrEmpty(snapshot.StopMessage) ? snapshot.Status : snapshot.StopMessage;
        PaintPending(snapshot.View.PendingMatchTile, snapshot.QuarterTurns);
        PaintSecrets(snapshot);
        PaintBoard(snapshot);
        PaintClaims(snapshot.View);
        PaintRotation(snapshot);
        _conceal.gameObject.SetActive(snapshot.ConcealVisible);
        if (snapshot.ConcealVisible)
        {
            _concealLabel.text = "Seat " + snapshot.View.CurrentSeat.Value + ", confirm to see your missions.";
        }
    }

    private void BuildCanvas()
    {
        EnsureEventSystem();
        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        _root = canvasGo.GetComponent<RectTransform>();
        var background = Ui.Image("Background", _root, SplatPalette.Cream);
        Ui.Stretch(background.rectTransform);

        _play = Ui.Rect("Play", _root);
        Ui.Stretch(_play);
        _status = Ui.Label("Status", _play, 28, SplatPalette.Ink, TextAnchor.MiddleLeft);
        Ui.Anchored(_status.rectTransform, new Vector2(0.02f, 0.92f), new Vector2(0.98f, 0.99f), Vector2.zero, Vector2.zero);

        _pending = Ui.Rect("Pending", _play);
        Ui.Anchored(_pending, new Vector2(0.02f, 0.72f), new Vector2(0.18f, 0.91f), Vector2.zero, Vector2.zero);
        _secrets = Ui.Rect("Secrets", _play);
        Ui.Anchored(_secrets, new Vector2(0.20f, 0.72f), new Vector2(0.72f, 0.91f), Vector2.zero, Vector2.zero);
        _rotation = Ui.Rect("Rotation", _play);
        Ui.Anchored(_rotation, new Vector2(0.74f, 0.72f), new Vector2(0.98f, 0.91f), Vector2.zero, Vector2.zero);
        _board = Ui.Rect("Board", _play);
        Ui.Anchored(_board, new Vector2(0.02f, 0.18f), new Vector2(0.98f, 0.70f), Vector2.zero, Vector2.zero);
        var boardFill = Ui.Image("BoardFill", _board, Color.white);
        Ui.Stretch(boardFill.rectTransform);
        boardFill.color = new Color(1f, 1f, 1f, 0.35f);
        _claims = Ui.Rect("Claims", _play);
        Ui.Anchored(_claims, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.16f), Vector2.zero, Vector2.zero);

        _conceal = Ui.Rect("Conceal", _play);
        Ui.Anchored(_conceal, new Vector2(0.20f, 0.72f), new Vector2(0.72f, 0.91f), Vector2.zero, Vector2.zero);
        var concealFill = Ui.Image("Fill", _conceal, SplatPalette.Overlay);
        Ui.Stretch(concealFill.rectTransform);
        concealFill.raycastTarget = true;
        _concealLabel = Ui.Label("Message", _conceal, 22, Color.white, TextAnchor.MiddleCenter);
        Ui.Anchored(_concealLabel.rectTransform, new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero);
        var confirm = Ui.Button("Confirm", _conceal, "Confirm", SplatPalette.Green);
        Ui.Anchored(confirm.GetComponent<RectTransform>(), new Vector2(0.30f, 0.08f), new Vector2(0.70f, 0.38f), Vector2.zero, Vector2.zero);
        confirm.onClick.AddListener(() => Confirmed?.Invoke());
        _conceal.gameObject.SetActive(false);

        _setup = Ui.Rect("Setup", _root);
        Ui.Stretch(_setup);
        _play.gameObject.SetActive(false);
    }

    private void RebuildSetup()
    {
        Ui.Clear(_setup);
        var panel = Ui.Image("Panel", _setup, Color.white);
        Ui.Anchored(panel.rectTransform, new Vector2(0.22f, 0.12f), new Vector2(0.78f, 0.88f), Vector2.zero, Vector2.zero);
        var title = Ui.Label("Title", panel.transform, 36, SplatPalette.Ink, TextAnchor.MiddleCenter);
        Ui.Anchored(title.rectTransform, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
        title.text = "Mission Splat";
        var seats = Ui.Label("Seats", panel.transform, 22, SplatPalette.Ink, TextAnchor.MiddleLeft);
        Ui.Anchored(seats.rectTransform, new Vector2(0.08f, 0.74f), new Vector2(0.40f, 0.84f), Vector2.zero, Vector2.zero);
        seats.text = "Seats";
        for (var count = 2; count <= 4; count++)
        {
            var chosen = count;
            var button = Ui.Button("Seats" + count, panel.transform, count.ToString(), chosen == _draft.SeatCount ? SplatPalette.Green : SplatPalette.Muted);
            var x = 0.42f + ((count - 2) * 0.16f);
            Ui.Anchored(button.GetComponent<RectTransform>(), new Vector2(x, 0.74f), new Vector2(x + 0.14f, 0.84f), Vector2.zero, Vector2.zero);
            button.onClick.AddListener(() => ReplaceDraft(chosen, ClampFlags(chosen), Mathf.Min(_draft.FirstSeatIndex, chosen - 1), _draft.Shuffle));
        }

        for (var i = 0; i < _draft.SeatCount; i++)
        {
            var index = i;
            var row = 0.58f - (i * 0.10f);
            var label = Ui.Label("Seat" + i, panel.transform, 20, SplatPalette.Ink, TextAnchor.MiddleLeft);
            Ui.Anchored(label.rectTransform, new Vector2(0.08f, row), new Vector2(0.28f, row + 0.09f), Vector2.zero, Vector2.zero);
            label.text = "Seat " + (i + 1);
            var human = Ui.Button("Human" + i, panel.transform, "Human", _draft.IsAi(i) ? SplatPalette.Muted : SplatPalette.Blue);
            Ui.Anchored(human.GetComponent<RectTransform>(), new Vector2(0.30f, row), new Vector2(0.50f, row + 0.09f), Vector2.zero, Vector2.zero);
            human.onClick.AddListener(() => SetAi(index, false));
            var ai = Ui.Button("Ai" + i, panel.transform, "AI", _draft.IsAi(i) ? SplatPalette.Purple : SplatPalette.Muted);
            Ui.Anchored(ai.GetComponent<RectTransform>(), new Vector2(0.52f, row), new Vector2(0.72f, row + 0.09f), Vector2.zero, Vector2.zero);
            ai.onClick.AddListener(() => SetAi(index, true));
            var first = Ui.Button("First" + i, panel.transform, "First", _draft.FirstSeatIndex == i ? SplatPalette.Green : SplatPalette.Muted);
            Ui.Anchored(first.GetComponent<RectTransform>(), new Vector2(0.74f, row), new Vector2(0.92f, row + 0.09f), Vector2.zero, Vector2.zero);
            first.onClick.AddListener(() => ReplaceDraft(_draft.SeatCount, CopyFlags(), index, _draft.Shuffle));
        }

        var shuffle = Ui.Button("Shuffle", panel.transform, _draft.Shuffle ? "Shuffle on" : "Shuffle off", _draft.Shuffle ? SplatPalette.Green : SplatPalette.Muted);
        Ui.Anchored(shuffle.GetComponent<RectTransform>(), new Vector2(0.08f, 0.08f), new Vector2(0.40f, 0.18f), Vector2.zero, Vector2.zero);
        shuffle.onClick.AddListener(() => ReplaceDraft(_draft.SeatCount, CopyFlags(), _draft.FirstSeatIndex, !_draft.Shuffle));
        var start = Ui.Button("Start", panel.transform, "Start", SplatPalette.Green);
        Ui.Anchored(start.GetComponent<RectTransform>(), new Vector2(0.50f, 0.08f), new Vector2(0.92f, 0.18f), Vector2.zero, Vector2.zero);
        start.onClick.AddListener(() => Started?.Invoke(_draft));
    }

    private void PaintPending(Tile pending, int quarterTurns)
    {
        Ui.Clear(_pending);
        var title = Ui.Label("Title", _pending, 18, SplatPalette.Muted, TextAnchor.UpperLeft);
        Ui.Anchored(title.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        title.text = "Pending tile";
        if (pending is null)
        {
            return;
        }

        PaintTile(_pending, pending, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.80f), quarterTurns);
    }

    private void PaintSecrets(TableSnapshot snapshot)
    {
        Ui.Clear(_secrets);
        var title = Ui.Label("Title", _secrets, 18, SplatPalette.Muted, TextAnchor.UpperLeft);
        Ui.Anchored(title.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        title.text = snapshot.SecretsVisible ? "Seat " + snapshot.View.Seat.Value + " missions" : "Missions hidden";
        if (!snapshot.SecretsVisible)
        {
            return;
        }

        var missions = snapshot.View.UnclaimedMissions;
        for (var i = 0; i < missions.Count; i++)
        {
            var card = Ui.Image("Mission" + i, _secrets, Color.white);
            var min = new Vector2(0.02f + (i * 0.48f), 0.02f);
            var max = new Vector2(0.46f + (i * 0.48f), 0.80f);
            Ui.Anchored(card.rectTransform, min, max, Vector2.zero, Vector2.zero);
            CellPainter.PaintMission(card.rectTransform, missions[i]);
        }
    }

    private void PaintBoard(TableSnapshot snapshot)
    {
        for (var i = _board.childCount - 1; i >= 1; i--)
        {
            DestroyImmediate(_board.GetChild(i).gameObject);
        }

        var view = snapshot.View;
        if (view.Board.Cells.Count == 0)
        {
            return;
        }

        var minX = int.MaxValue;
        var maxX = int.MinValue;
        var minY = int.MaxValue;
        var maxY = int.MinValue;
        foreach (var occupiedCell in view.Board.Cells)
        {
            minX = Math.Min(minX, occupiedCell.CellX);
            maxX = Math.Max(maxX, occupiedCell.CellX);
            minY = Math.Min(minY, occupiedCell.CellY);
            maxY = Math.Max(maxY, occupiedCell.CellY);
        }

        foreach (var highlight in snapshot.Highlights)
        {
            minX = Math.Min(minX, highlight.TileX * 2);
            maxX = Math.Max(maxX, (highlight.TileX * 2) + 1);
            minY = Math.Min(minY, highlight.TileY * 2);
            maxY = Math.Max(maxY, (highlight.TileY * 2) + 1);
        }

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        var area = _board.rect;
        var cellSize = Math.Min(area.width / (width + 1), area.height / (height + 1));
        if (cellSize <= 1f)
        {
            cellSize = 36f;
        }

        var lattice = Ui.Rect("Lattice", _board);
        lattice.anchorMin = new Vector2(0.5f, 0.5f);
        lattice.anchorMax = new Vector2(0.5f, 0.5f);
        lattice.pivot = new Vector2(0.5f, 0.5f);
        lattice.sizeDelta = new Vector2(width * cellSize, height * cellSize);

        foreach (var tile in view.Board.Tiles)
        {
            var back = Ui.Image("Tile" + tile.Id.Value, lattice, SplatPalette.Tile);
            PlaceCell(back.rectTransform, tile.TileX * 2, tile.TileY * 2, 2, 2, minX, minY, cellSize, 0f, 0f);
        }

        foreach (var occupied in view.Board.Cells)
        {
            var cellRect = Ui.Rect("Cell" + occupied.CellX + "_" + occupied.CellY, lattice);
            PlaceCell(cellRect, occupied.CellX, occupied.CellY, 1, 1, minX, minY, cellSize, 0f, 0f);
            CellPainter.Paint(cellRect, occupied.Value);
        }

        if (snapshot.ConcealVisible)
        {
            return;
        }

        foreach (var highlight in snapshot.Highlights)
        {
            var button = Ui.Button("Highlight" + highlight.TileX + "_" + highlight.TileY, lattice, string.Empty, SplatPalette.Highlight);
            PlaceCell(button.GetComponent<RectTransform>(), highlight.TileX * 2, highlight.TileY * 2, 2, 2, minX, minY, cellSize, 0f, 0f);
            var chosen = highlight;
            button.onClick.AddListener(() => Tapped?.Invoke(chosen));
        }
    }

    private void PaintClaims(SeatView view)
    {
        Ui.Clear(_claims);
        var title = Ui.Label("Title", _claims, 18, SplatPalette.Muted, TextAnchor.UpperLeft);
        Ui.Anchored(title.rectTransform, new Vector2(0f, 0.78f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        title.text = "Claims";
        var seats = view.Claims.Count;
        for (var i = 0; i < seats; i++)
        {
            var row = view.Claims[i];
            var min = new Vector2(i / (float)seats, 0f);
            var max = new Vector2((i + 1) / (float)seats, 0.76f);
            var group = Ui.Rect("Seat" + row.Seat.Value, _claims);
            Ui.Anchored(group, min, max, new Vector2(6f, 0f), new Vector2(-6f, 0f));
            var label = Ui.Label("Label", group, 16, SplatPalette.Ink, TextAnchor.UpperLeft);
            Ui.Anchored(label.rectTransform, new Vector2(0f, 0.72f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            label.text = "Seat " + row.Seat.Value;
            for (var m = 0; m < row.Missions.Count; m++)
            {
                var card = Ui.Image("Claim" + m, group, Color.white);
                var left = 0.02f + (m * 0.23f);
                Ui.Anchored(card.rectTransform, new Vector2(left, 0.02f), new Vector2(left + 0.21f, 0.70f), Vector2.zero, Vector2.zero);
                CellPainter.PaintMission(card.rectTransform, row.Missions[m]);
            }
        }
    }

    private void PaintRotation(TableSnapshot snapshot)
    {
        Ui.Clear(_rotation);
        var title = Ui.Label("Title", _rotation, 18, SplatPalette.Muted, TextAnchor.UpperLeft);
        Ui.Anchored(title.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        title.text = "Quarter-turns";
        for (var turn = 0; turn < 4; turn++)
        {
            var chosen = turn;
            var fill = turn == snapshot.QuarterTurns ? SplatPalette.Green : SplatPalette.Muted;
            var button = Ui.Button("Turn" + turn, _rotation, turn.ToString(), fill);
            var x = turn / 4f;
            Ui.Anchored(button.GetComponent<RectTransform>(), new Vector2(x + 0.02f, 0.15f), new Vector2(x + 0.23f, 0.75f), Vector2.zero, Vector2.zero);
            button.interactable = !snapshot.ConcealVisible && string.IsNullOrEmpty(snapshot.StopMessage);
            button.onClick.AddListener(() => QuarterTurnsSelected?.Invoke(chosen));
        }
    }

    private static void PaintTile(RectTransform parent, Tile tile, Vector2 min, Vector2 max, int quarterTurns)
    {
        var frame = Ui.Image("Tile", parent, SplatPalette.Tile);
        Ui.Anchored(frame.rectTransform, min, max, Vector2.zero, Vector2.zero);
        Canvas.ForceUpdateCanvases();
        var area = frame.rectTransform.rect;
        var cell = Math.Min(area.width, area.height) * 0.42f;
        if (cell <= 1f)
        {
            cell = 36f;
        }

        var lattice = Ui.Rect("Lattice", frame.transform);
        lattice.anchorMin = new Vector2(0.5f, 0.5f);
        lattice.anchorMax = new Vector2(0.5f, 0.5f);
        lattice.pivot = new Vector2(0.5f, 0.5f);
        lattice.sizeDelta = new Vector2(cell * 2f, cell * 2f);
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                var cellRect = Ui.Rect("C" + x + y, lattice);
                // Same clockwise map as Rules.Tile: (x, y) goes to (y, 1 - x).
                var (turnedX, turnedY) = TurnClockwise(x, y, quarterTurns);
                PlaceCell(cellRect, turnedX, turnedY, 1, 1, 0, 0, cell, 0f, 0f);
                CellPainter.Paint(cellRect, tile.Local(x, y));
            }
        }
    }

    private static (int X, int Y) TurnClockwise(int x, int y, int quarterTurns) =>
        quarterTurns switch
        {
            0 => (x, y),
            1 => (y, 1 - x),
            2 => (1 - x, 1 - y),
            3 => (1 - y, x),
            _ => throw new ArgumentOutOfRangeException(nameof(quarterTurns), quarterTurns, "Quarter-turns are 0, 1, 2, or 3."),
        };

    private static void PlaceCell(
        RectTransform rect,
        int cellX,
        int cellY,
        int cellsWide,
        int cellsHigh,
        int minX,
        int minY,
        float cell,
        float originX,
        float originY)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(cellsWide * cell, cellsHigh * cell);
        rect.anchoredPosition = new Vector2(originX + ((cellX - minX) * cell), originY + ((cellY - minY) * cell));
    }

    private void SetAi(int index, bool ai)
    {
        var flags = CopyFlags();
        flags[index] = ai;
        ReplaceDraft(_draft.SeatCount, flags, _draft.FirstSeatIndex, _draft.Shuffle);
    }

    private void ReplaceDraft(int seatCount, bool[] flags, int first, bool shuffle)
    {
        _draft = new TableStart(seatCount, flags, first, shuffle);
        RebuildSetup();
    }

    private bool[] CopyFlags()
    {
        var flags = new bool[_draft.SeatCount];
        for (var i = 0; i < flags.Length; i++)
        {
            flags[i] = _draft.IsAi(i);
        }

        return flags;
    }

    private bool[] ClampFlags(int seatCount)
    {
        var flags = new bool[seatCount];
        for (var i = 0; i < seatCount; i++)
        {
            flags[i] = i < _draft.SeatCount && _draft.IsAi(i);
        }

        return flags;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.transform.SetParent(transform, false);
    }
}
}
