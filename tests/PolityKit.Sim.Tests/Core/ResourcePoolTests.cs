using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Tests.Core;

public sealed class ResourcePoolTests
{
    public static TheoryData<ResourceKind, int> Resources => new()
    {
        { ResourceKind.Food, 10 },
        { ResourceKind.Medicine, 20 },
        { ResourceKind.Housing, 30 },
        { ResourceKind.AdminCapacity, 40 },
        { ResourceKind.ProductionCapacity, 50 }
    };

    [Theory]
    [MemberData(nameof(Resources))]
    public void SetAndGetRoundTripForEachResourceKind(ResourceKind resource, int amount)
    {
        var pool = new ResourcePool();

        pool.Set(resource, amount);

        Assert.Equal(amount, pool.Get(resource));
    }

    [Fact]
    public void TryConsumeReducesAvailableResourceWhenEnoughExists()
    {
        var pool = new ResourcePool { Food = 12 };

        var consumed = pool.TryConsume(ResourceKind.Food, 5);

        Assert.True(consumed);
        Assert.Equal(7, pool.Food);
    }

    [Fact]
    public void TryConsumeReturnsFalseAndLeavesResourceUnchangedWhenInsufficient()
    {
        var pool = new ResourcePool { Medicine = 4 };

        var consumed = pool.TryConsume(ResourceKind.Medicine, 5);

        Assert.False(consumed);
        Assert.Equal(4, pool.Medicine);
    }

    [Fact]
    public void TryConsumeAllowsZeroAmount()
    {
        var pool = new ResourcePool { Housing = 3 };

        var consumed = pool.TryConsume(ResourceKind.Housing, 0);

        Assert.True(consumed);
        Assert.Equal(3, pool.Housing);
    }

    [Fact]
    public void TryConsumeRejectsNegativeAmount()
    {
        var pool = new ResourcePool();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            pool.TryConsume(ResourceKind.Food, -1));
    }
}
