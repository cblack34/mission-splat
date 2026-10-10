namespace MissionSplat.Rules.Tests;

[TestFixture]
public sealed class SpentUsesTests
{
    [Test]
    public void AFreshTurnHasSpentNothing()
    {
        SpentUses fresh = default;

        Assert.That(fresh.Of(OrdinaryCatalog.Rotate), Is.EqualTo(0));
        Assert.That(fresh.Of(OrdinaryCatalog.Bounce), Is.EqualTo(0));
        Assert.That(fresh.Of(new SymbolId("unlisted")), Is.EqualTo(0));
    }

    [Test]
    public void SpendingReturnsANewValueAndLeavesTheOriginalUnchanged()
    {
        SpentUses fresh = default;
        var spent = fresh.WithUse(OrdinaryCatalog.Rotate);

        Assert.That(fresh.Of(OrdinaryCatalog.Rotate), Is.EqualTo(0));
        Assert.That(spent.Of(OrdinaryCatalog.Rotate), Is.EqualTo(1));
        Assert.That(spent.Of(OrdinaryCatalog.Bounce), Is.EqualTo(0));
    }

    [Test]
    public void UsesOfDifferentPowersAreCountedIndependently()
    {
        var spent = default(SpentUses)
            .WithUse(OrdinaryCatalog.Rotate)
            .WithUse(OrdinaryCatalog.Bounce)
            .WithUse(OrdinaryCatalog.Rotate);

        Assert.That(spent.Of(OrdinaryCatalog.Rotate), Is.EqualTo(2));
        Assert.That(spent.Of(OrdinaryCatalog.Bounce), Is.EqualTo(1));
    }
}
