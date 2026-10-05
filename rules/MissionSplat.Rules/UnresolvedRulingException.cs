namespace MissionSplat.Rules;

public sealed class UnresolvedRulingException : Exception
{
    public UnresolvedRulingException(string message)
        : base(message)
    {
    }
}
