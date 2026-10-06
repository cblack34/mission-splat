namespace MissionSplat.Rules;

public enum MissionPattern
{
    Row,
    Square,
    L,
}

public sealed record Mission(MissionId Id, MissionPattern Pattern, ColorId Color);
