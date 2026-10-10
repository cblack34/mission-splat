namespace MissionSplat.Player
{

using System;
using MissionSplat.App;
using UnityEngine;
using UnityEngine.UI;

internal sealed class TableSetupView
{
    private readonly RectTransform _root;
    private TableStart _draft;

    public event Action<TableStart> Started;

    public TableSetupView(RectTransform root)
    {
        _root = root;
    }

    public void Show(TableStart draft)
    {
        _draft = draft;
        Rebuild();
    }

    private void Rebuild()
    {
        Ui.Clear(_root);
        var panel = Ui.Image("Panel", _root, Color.white);
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

    private void SetAi(int index, bool ai)
    {
        var flags = CopyFlags();
        flags[index] = ai;
        ReplaceDraft(_draft.SeatCount, flags, _draft.FirstSeatIndex, _draft.Shuffle);
    }

    private void ReplaceDraft(int seatCount, bool[] flags, int first, bool shuffle)
    {
        _draft = new TableStart(seatCount, flags, first, shuffle);
        Rebuild();
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
}
}
