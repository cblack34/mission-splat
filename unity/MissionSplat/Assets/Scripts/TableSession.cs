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
        _view.Started += Begin;
    }

    public void Begin(TableStart start)
    {
        var session = new LocalSession();
        Begin(new Table(session, start), session);
    }

    public void Begin(TableStart start, GameSetup setup)
    {
        var session = new LocalSession();
        Begin(new Table(session, start, setup), session);
    }

    public void Tap(int tileX, int tileY) => Forward(() => _table.Submit(new Place(tileX, tileY, _table.Snapshot.QuarterTurns)));

    public void ConfirmIncomingSeat() => Forward(() => _table.Confirm());

    public void SetQuarterTurns(int quarterTurns) => Forward(() => _table.SetQuarterTurns(quarterTurns));

    private void Begin(Table table, ISession session)
    {
        table.Start();
        _table = table;
        _view.NotePowers(session.PowersInPlay);
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
