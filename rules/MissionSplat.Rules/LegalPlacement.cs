namespace MissionSplat.Rules;

// Beside is an empty position the drawn tile may be set down on; OnTop covers a tile already there (stack).
public enum PlacementKind
{
    Beside,
    OnTop,
}

public readonly record struct LegalPlacement(int TileX, int TileY, PlacementKind Kind);

public readonly record struct BoardPosition(int TileX, int TileY);
