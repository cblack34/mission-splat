namespace MissionSplat.App;

using MissionSplat.Rules;

// The turn driver: it runs the automated seats, hands the device between seats, and builds what a GUI draws.
// It talks to the match only through the session, so any ISession adapter fits.
public sealed class Table
{
    private const int AutomatedMoveLimit = 64;

    private readonly ISession _session;
    private readonly TableStart _start;
    private readonly GameSetup _setup;
    private readonly IPlayer?[] _players;
    private TableSnapshot? _snapshot;
    private int _quarterTurns;
    private bool _conceal;
    private SeatId? _winner;
    private string? _stop;

    public Table(ISession session, TableStart start, int? shuffleSeed = null)
        : this(session, start, OrdinarySetup(start, shuffleSeed))
    {
    }

    public Table(ISession session, TableStart start, GameSetup setup)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _start = start ?? throw new ArgumentNullException(nameof(start));
        _setup = setup ?? throw new ArgumentNullException(nameof(setup));
        RequireSameSeats(start, setup);
        _players = BindPlayers(session, start);
    }

    public bool IsStarted => _snapshot is not null;

    public TableSnapshot Snapshot =>
        _snapshot ?? throw new InvalidOperationException("The table has not started.");

    public void Start()
    {
        var started = _session.Start(_setup);
        if (!started.IsAccepted)
        {
            throw new InvalidOperationException(started.Rejection?.Message ?? "The table could not start.");
        }

        _quarterTurns = 0;
        _winner = null;
        _stop = null;
        _conceal = IsHuman(_session.CurrentSeat);
        RunAutomatedSeats();
        Refresh();
    }

    // The current human seat's action. A tap while nobody may act is refused, not an error.
    public SessionResult Submit(GameAction action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (!CanAct())
        {
            return SessionResult.Reject(new SessionRejection("The table is not taking an action.", null));
        }

        var result = Apply(_session.CurrentSeat, action);
        if (result.IsAccepted)
        {
            RunAutomatedSeats();
        }

        Refresh();
        return result;
    }

    // The incoming human seat holds the device, so its hand may be drawn.
    public void Confirm()
    {
        if (!IsStarted || !_conceal || IsFinished())
        {
            return;
        }

        _conceal = false;
        Refresh();
    }

    public void SetQuarterTurns(int quarterTurns)
    {
        if (quarterTurns is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(quarterTurns), quarterTurns, "Quarter-turns are 0, 1, 2, or 3.");
        }

        if (!IsStarted || IsFinished())
        {
            return;
        }

        _quarterTurns = quarterTurns;
        Refresh();
    }

    private bool CanAct() => IsStarted && !IsFinished() && !_conceal && IsHuman(_session.CurrentSeat);

    private bool IsFinished() => _stop is not null || _winner is not null;

    private bool IsHuman(SeatId seat) => _players[_start.IndexOf(seat)] is null;

    private void RunAutomatedSeats()
    {
        var moves = 0;
        while (!IsFinished() && _players[_start.IndexOf(_session.CurrentSeat)] is { } player)
        {
            moves++;
            if (moves > AutomatedMoveLimit)
            {
                Stop("AI play did not return to a human seat.");
                return;
            }

            GameAction chosen;
            try
            {
                chosen = player.ChooseAction(_session.View(player.Seat));
            }
            catch (Exception ex) when (ex is UnresolvedRulingException or InvalidOperationException)
            {
                Stop(ex.Message);
                return;
            }

            var result = Apply(player.Seat, chosen);
            if (!result.IsAccepted)
            {
                if (_stop is null)
                {
                    Stop(result.Rejection?.Message ?? string.Empty);
                }

                return;
            }
        }
    }

    private SessionResult Apply(SeatId seat, GameAction action)
    {
        SessionResult result;
        try
        {
            result = _session.Submit(seat, action);
        }
        catch (UnresolvedRulingException ex)
        {
            Stop(ex.Message);
            return SessionResult.Reject(new SessionRejection(ex.Message, null));
        }

        if (!result.IsAccepted)
        {
            return result;
        }

        if (action is Place)
        {
            _quarterTurns = 0;
        }

        foreach (var won in result.Events.OfType<GameWon>())
        {
            _winner = won.Seat;
        }

        // A power use leaves the acting seat current, so only a move to another seat hides a hand.
        var next = _session.CurrentSeat;
        if (_winner is null && !next.Equals(seat) && IsHuman(next))
        {
            _conceal = true;
        }

        return result;
    }

    private void Stop(string message)
    {
        _stop = message;
        _conceal = false;
    }

    private void Refresh()
    {
        var seat = _session.CurrentSeat;
        var view = _session.View(seat);
        var human = IsHuman(seat);
        var canAct = human && !_conceal && !view.HasEnded && _stop is null;
        _snapshot = new TableSnapshot(
            view,
            secretsVisible: human && !_conceal && !view.HasEnded,
            concealVisible: _conceal,
            canAct ? _session.LegalPlacements(seat, _quarterTurns) : [],
            canAct ? TargetsFor(seat, view) : [],
            _quarterTurns,
            StatusFor(seat, view));
    }

    private PowerTargets[] TargetsFor(SeatId seat, SeatView view)
    {
        var targets = new PowerTargets[view.RemainingUses.Count];
        for (var i = 0; i < targets.Length; i++)
        {
            var power = view.RemainingUses[i].Power;
            targets[i] = new PowerTargets(power, _session.LegalTargets(seat, power));
        }

        return targets;
    }

    private TableStatus StatusFor(SeatId seat, SeatView view)
    {
        if (_stop is not null)
        {
            return TableStatus.Stopped(_stop);
        }

        if (view.HasEnded)
        {
            return _winner is { } winner ? TableStatus.Won(winner) : TableStatus.Ended();
        }

        return _conceal ? TableStatus.ToConfirm(seat) : TableStatus.ToAct(seat);
    }

    private static GameSetup OrdinarySetup(TableStart start, int? shuffleSeed)
    {
        if (start is null)
        {
            throw new ArgumentNullException(nameof(start));
        }

        // The toggle asks for a shuffle; the seed only makes it reproducible, and a fresh one is drawn when none is given.
        int? seed = start.Shuffle ? shuffleSeed ?? new Random().Next() : null;
        return TableDeck.Ordinary(start.SeatsInTurnOrder(), start.SeatAt(start.FirstSeatIndex), seed);
    }

    private static void RequireSameSeats(TableStart start, GameSetup setup)
    {
        var seats = start.SeatsInTurnOrder();
        var same = setup.SeatsInTurnOrder.Count == seats.Length && setup.FirstSeat.Equals(start.SeatAt(start.FirstSeatIndex));
        for (var i = 0; same && i < seats.Length; i++)
        {
            same = setup.SeatsInTurnOrder[i].Equals(seats[i]);
        }

        if (!same)
        {
            throw new ArgumentException("The setup's seats and first seat have to match the table start.", nameof(setup));
        }
    }

    private static IPlayer?[] BindPlayers(ISession session, TableStart start)
    {
        var players = new IPlayer?[start.SeatCount];
        for (var i = 0; i < players.Length; i++)
        {
            if (start.IsAi(i))
            {
                var seat = start.SeatAt(i);
                players[i] = new AiPlayer(
                    seat,
                    quarterTurns => session.LegalPlacements(seat, quarterTurns),
                    action => session.Preview(seat, action));
            }
        }

        return players;
    }
}
