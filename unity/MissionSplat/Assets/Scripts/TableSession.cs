namespace MissionSplat.Player
{

using System;
using System.Collections.Generic;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class TableSession : MonoBehaviour
{
    private static readonly (int X, int Y)[] NeighborSteps =
    {
        (1, 0),
        (-1, 0),
        (0, 1),
        (0, -1),
    };

    private ISession _session;
    private IPlayer[] _players;
    private TableStart _start;
    private TableView _view;
    private int _quarterTurns;
    private bool _conceal;
    private string _stop = string.Empty;

    public bool IsStarted => _session != null;

    public TableSnapshot Snapshot { get; private set; }

    public SeatView View => Snapshot.View;

    public IReadOnlyList<Placement> Highlights => Snapshot.Highlights;

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
        if (start is null)
        {
            throw new ArgumentNullException(nameof(start));
        }

        var session = new LocalSession();
        var setup = TableDeck.Ordinary(start.SeatsInTurnOrder(), start.SeatAt(start.FirstSeatIndex), start.Shuffle);
        var started = session.Start(setup);
        if (!started.IsAccepted)
        {
            throw new InvalidOperationException(started.Rejection?.Message ?? "The table could not start.");
        }

        _session = session;
        _start = start;
        _players = BindPlayers(session, start);
        _quarterTurns = 0;
        _stop = string.Empty;
        EnterCurrentSeat();
        PlayAiUntilHumanOrEnd();
        Refresh();
    }

    public void ConfirmIncomingSeat()
    {
        if (!IsStarted || !string.IsNullOrEmpty(_stop) || View.HasEnded)
        {
            return;
        }

        if (!_conceal || CurrentIsAi())
        {
            return;
        }

        _conceal = false;
        Refresh();
    }

    public void Tap(Placement placement)
    {
        if (!IsStarted || !string.IsNullOrEmpty(_stop) || View.HasEnded || _conceal)
        {
            return;
        }

        if (CurrentIsAi())
        {
            return;
        }

        var human = (HumanPlayer)CurrentPlayer();
        var view = _session.View(human.Seat);
        human.SubmitTap(placement);
        Place(human.Seat, human.ChoosePlacement(view));
        PlayAiUntilHumanOrEnd();
        Refresh();
    }

    public void SetQuarterTurns(int quarterTurns)
    {
        if (quarterTurns is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(quarterTurns), quarterTurns, "Quarter-turns are 0, 1, 2, or 3.");
        }

        _quarterTurns = quarterTurns;
        if (IsStarted)
        {
            Refresh();
        }
    }

    private void PlayAiUntilHumanOrEnd()
    {
        var guard = 0;
        while (IsStarted && string.IsNullOrEmpty(_stop) && !CurrentView().HasEnded && CurrentIsAi())
        {
            guard++;
            if (guard > 64)
            {
                Stop("AI play did not return to a human seat.");
                return;
            }

            var player = CurrentPlayer();
            var view = _session.View(player.Seat);
            Placement chosen;
            try
            {
                chosen = player.ChoosePlacement(view);
            }
            catch (InvalidOperationException ex)
            {
                Stop(ex.Message);
                return;
            }

            Place(player.Seat, chosen);
        }
    }

    private void Place(SeatId seat, Placement placement)
    {
        try
        {
            var result = _session.Place(seat, placement);
            if (!result.IsAccepted)
            {
                return;
            }
        }
        catch (UnresolvedRulingException ex)
        {
            Stop(ex.Message);
            return;
        }

        _quarterTurns = 0;
        EnterCurrentSeat();
    }

    private void EnterCurrentSeat()
    {
        if (!IsStarted)
        {
            return;
        }

        var view = CurrentView();
        _conceal = !view.HasEnded && !CurrentIsAi();
    }

    private void Refresh()
    {
        if (!IsStarted)
        {
            return;
        }

        var view = CurrentView();
        var highlights = _conceal || view.HasEnded || !string.IsNullOrEmpty(_stop)
            ? Array.Empty<Placement>()
            : FindHighlights(view, _quarterTurns);
        Snapshot = new TableSnapshot(
            view,
            secretsVisible: !_conceal && !CurrentIsAi() && !view.HasEnded,
            concealVisible: _conceal,
            highlights,
            _quarterTurns,
            Status(view),
            _stop);
        _view.Render(Snapshot);
    }

    private IReadOnlyList<Placement> FindHighlights(SeatView view, int quarterTurns)
    {
        var found = new List<Placement>();
        var seen = new HashSet<(int X, int Y)>();
        foreach (var tile in view.Board.Tiles)
        {
            foreach (var step in NeighborSteps)
            {
                var tileX = tile.TileX + step.X;
                var tileY = tile.TileY + step.Y;
                if (!seen.Add((tileX, tileY)))
                {
                    continue;
                }

                var placement = new Placement(tileX, tileY, quarterTurns);
                PlacementPreview preview;
                try
                {
                    preview = _session.Preview(view.Seat, placement);
                }
                catch (UnresolvedRulingException ex)
                {
                    Stop(ex.Message);
                    return Array.Empty<Placement>();
                }

                if (preview.IsAccepted)
                {
                    found.Add(placement);
                }
            }
        }

        return found;
    }

    private SeatView CurrentView()
    {
        var first = _session.View(_start.SeatAt(0));
        return _session.View(first.CurrentSeat);
    }

    private IPlayer CurrentPlayer()
    {
        var seat = CurrentView().CurrentSeat;
        return _players[_start.IndexOf(seat)];
    }

    private bool CurrentIsAi() => _start.IsAi(_start.IndexOf(CurrentView().CurrentSeat));

    private string Status(SeatView view)
    {
        if (!string.IsNullOrEmpty(_stop))
        {
            return _stop;
        }

        if (view.HasEnded)
        {
            foreach (var row in view.Claims)
            {
                if (row.Missions.Count >= OrdinaryCatalog.ClaimsRequiredToWin)
                {
                    return "Seat " + row.Seat.Value + " wins.";
                }
            }

            return "The game has ended.";
        }

        return _conceal
            ? "Seat " + view.CurrentSeat.Value + " to confirm."
            : "Seat " + view.CurrentSeat.Value + " to place.";
    }

    private void Stop(string message)
    {
        _stop = message;
        _conceal = false;
    }

    private static IPlayer[] BindPlayers(ISession session, TableStart start)
    {
        var players = new IPlayer[start.SeatCount];
        for (var i = 0; i < start.SeatCount; i++)
        {
            var seat = start.SeatAt(i);
            if (start.IsAi(i))
            {
                players[i] = new AiPlayer(seat, placement => session.Preview(seat, placement));
            }
            else
            {
                players[i] = new HumanPlayer(seat);
            }
        }

        return players;
    }
}
}
