namespace MissionSplat.Rules;

// The internal constructor closes the set: only this assembly adds an action, so a new one is a compiler-visible change.
public abstract record GameAction
{
    internal GameAction()
    {
    }
}

// One rotate cell on the drawn tile, spent on a tile already on the board. Zero quarter-turns is not a rotate.
public sealed record UseRotate(int TileX, int TileY, int QuarterTurnsClockwise) : GameAction;

public sealed record UseBounce(int TileX, int TileY) : GameAction;

// The drawn tile covers the 2×2 cells at (2*TileX + lx, 2*TileY + ly), y upward. Quarter-turns are clockwise and
// only orient this placement. On an occupied position the placement is the stack.
public sealed record Place(int TileX, int TileY, int QuarterTurnsClockwise) : GameAction;
