namespace MissionSplat.Player
{

using System;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;

// Forwards the view's input to the App table and draws its snapshot; it decides nothing about the game.
[DefaultExecutionOrder(-100)]
public sealed class TableSession : MonoBehaviour
{
    private Table _table;
    private TableView _view;

    public bool IsStarted => _table != null && _table.IsStarted;

    public TableSnapshot Snapshot => _table.Snapshot;

    private void Awake()
    {
        _view = GetComponent<TableView>();
        if (_view == null)
        {
            _view = gameObject.AddComponent<TableView>();
        }

        _view.Tapped += Tap;
        _view.Confirmed += ConfirmIncomingSeat;
        _view.QuarterTurnsSelected += SetQuarterTurns;
        _view.PowerSelected += SelectPower;
        _view.TargetTapped += UseAt;
        _view.Started += Begin;
    }

    // The shuffle seed is drawn here, at the edge, so the App table stays deterministic.
    public void Begin(TableStart start)
    {
        if (start is null)
        {
            throw new ArgumentNullException(nameof(start));
        }

        Begin(new Table(new LocalSession(), start, start.Shuffle ? new System.Random().Next() : (int?)null));
    }

    public void Begin(TableStart start, GameSetup setup) => Begin(new Table(new LocalSession(), start, setup));

    public void Tap(int tileX, int tileY) => Forward(() => _table.Place(tileX, tileY));

    public void SelectPower(SymbolId? power) => Forward(() => _table.SelectPower(power));

    public void UseAt(int tileX, int tileY) => Forward(() => _table.UseAt(tileX, tileY));

    public void Submit(GameAction action) => Forward(() => _table.Submit(action));

    public void ConfirmIncomingSeat() => Forward(() => _table.Confirm());

    public void SetQuarterTurns(int quarterTurns) => Forward(() => _table.SetQuarterTurns(quarterTurns));

    private void Begin(Table table)
    {
        table.Start();
        _table = table;
        _view.Render(_table.Snapshot);
    }

    private void Forward(Action act)
    {
        if (!IsStarted)
        {
            return;
        }

        act();
        _view.Render(_table.Snapshot);
    }
}
}
