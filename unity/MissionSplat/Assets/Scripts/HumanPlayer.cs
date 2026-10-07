namespace MissionSplat.Player
{

using System;
using MissionSplat.App;
using MissionSplat.Rules;

public sealed class HumanPlayer : IPlayer
{
    private Placement? _tap;

    public HumanPlayer(SeatId seat)
    {
        Seat = seat;
    }

    public SeatId Seat { get; }

    public bool HasTap => _tap.HasValue;

    public void SubmitTap(Placement placement)
    {
        _tap = placement;
    }

    // IPlayer.ChoosePlacement is synchronous. The table waits for a tap, then asks this player.
    public Placement ChoosePlacement(SeatView view)
    {
        if (view is null)
        {
            throw new ArgumentNullException(nameof(view));
        }

        if (!view.Seat.Equals(Seat))
        {
            throw new ArgumentException("The view is for a different seat.", nameof(view));
        }

        if (_tap is not Placement chosen)
        {
            throw new InvalidOperationException("The seat has not tapped a placement.");
        }

        _tap = null;
        return chosen;
    }
}
}
