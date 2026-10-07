namespace MissionSplat.Player
{

using System.Collections.Generic;
using MissionSplat.App;

public sealed class TableSnapshot
{
    public TableSnapshot(
        SeatView view,
        bool secretsVisible,
        bool concealVisible,
        IReadOnlyList<Placement> highlights,
        int quarterTurns,
        string status,
        string stopMessage)
    {
        View = view;
        SecretsVisible = secretsVisible;
        ConcealVisible = concealVisible;
        Highlights = highlights;
        QuarterTurns = quarterTurns;
        Status = status;
        StopMessage = stopMessage;
    }

    public SeatView View { get; }

    public bool SecretsVisible { get; }

    public bool ConcealVisible { get; }

    public IReadOnlyList<Placement> Highlights { get; }

    public int QuarterTurns { get; }

    public string Status { get; }

    public string StopMessage { get; }
}
}
