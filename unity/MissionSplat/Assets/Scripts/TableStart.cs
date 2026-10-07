namespace MissionSplat.Player
{

using System;
using System.Collections.Generic;
using MissionSplat.Rules;

public sealed class TableStart
{
    public TableStart(int seatCount, IReadOnlyList<bool> aiSeats, int firstSeatIndex, bool shuffle)
    {
        if (seatCount is < 2 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(seatCount), seatCount, "A table has 2, 3, or 4 seats.");
        }

        if (aiSeats is null)
        {
            throw new ArgumentNullException(nameof(aiSeats));
        }

        if (aiSeats.Count != seatCount)
        {
            throw new ArgumentException("Each seat needs a human or AI flag.", nameof(aiSeats));
        }

        if (firstSeatIndex < 0 || firstSeatIndex >= seatCount)
        {
            throw new ArgumentOutOfRangeException(nameof(firstSeatIndex), firstSeatIndex, "The first seat has to be at the table.");
        }

        var flags = new bool[seatCount];
        for (var i = 0; i < seatCount; i++)
        {
            flags[i] = aiSeats[i];
        }

        SeatCount = seatCount;
        AiSeats = flags;
        FirstSeatIndex = firstSeatIndex;
        Shuffle = shuffle;
    }

    public int SeatCount { get; }

    public IReadOnlyList<bool> AiSeats { get; }

    public int FirstSeatIndex { get; }

    public bool Shuffle { get; }

    public static TableStart PassAndPlayDraft() => new(2, new[] { false, false }, 0, true);

    public static TableStart HumanThenAi() => new(2, new[] { false, true }, 0, false);

    public SeatId SeatAt(int index) => new((index + 1).ToString());

    public SeatId[] SeatsInTurnOrder()
    {
        var seats = new SeatId[SeatCount];
        for (var i = 0; i < SeatCount; i++)
        {
            seats[i] = SeatAt(i);
        }

        return seats;
    }

    public bool IsAi(int index) => AiSeats[index];

    public int IndexOf(SeatId seat)
    {
        for (var i = 0; i < SeatCount; i++)
        {
            if (SeatAt(i).Equals(seat))
            {
                return i;
            }
        }

        throw new ArgumentException("That seat is not at the table.", nameof(seat));
    }
}
}
