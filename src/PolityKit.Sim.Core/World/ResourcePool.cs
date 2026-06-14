namespace PolityKit.Sim.Core.World;

public sealed class ResourcePool
{
    public int Food { get; set; }

    public int Medicine { get; set; }

    public int Housing { get; set; }

    public int AdminCapacity { get; set; }

    public int ProductionCapacity { get; set; }

    public int Get(ResourceKind resource)
    {
        return resource switch
        {
            ResourceKind.Food => Food,
            ResourceKind.Medicine => Medicine,
            ResourceKind.Housing => Housing,
            ResourceKind.AdminCapacity => AdminCapacity,
            ResourceKind.ProductionCapacity => ProductionCapacity,
            _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, null)
        };
    }

    public void Set(ResourceKind resource, int amount)
    {
        switch (resource)
        {
            case ResourceKind.Food:
                Food = amount;
                break;
            case ResourceKind.Medicine:
                Medicine = amount;
                break;
            case ResourceKind.Housing:
                Housing = amount;
                break;
            case ResourceKind.AdminCapacity:
                AdminCapacity = amount;
                break;
            case ResourceKind.ProductionCapacity:
                ProductionCapacity = amount;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(resource), resource, null);
        }
    }

    public bool TryConsume(ResourceKind resource, int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount cannot be negative.");
        }

        var available = Get(resource);
        if (available < amount)
        {
            return false;
        }

        Set(resource, available - amount);
        return true;
    }
}
