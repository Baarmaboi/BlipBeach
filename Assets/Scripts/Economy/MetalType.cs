public enum MetalType
{
    Iron = 0,
    Copper = 1,
    Brass = 2,
    Aluminum = 3,
    Lead = 4,
    Silver = 5,
    Gold = 6
}

public static class MetalTypes
{
    public static readonly MetalType[] All =
    {
        MetalType.Iron,
        MetalType.Copper,
        MetalType.Brass,
        MetalType.Aluminum,
        MetalType.Lead,
        MetalType.Silver,
        MetalType.Gold
    };

    public static readonly MetalType[] Common =
    {
        MetalType.Iron,
        MetalType.Copper,
        MetalType.Brass,
        MetalType.Aluminum,
        MetalType.Lead
    };

    public static bool IsPrecious(MetalType metal)
    {
        return metal == MetalType.Silver || metal == MetalType.Gold;
    }

    public static bool UsesDailyMarket(MetalType metal)
    {
        return !IsPrecious(metal);
    }

    public static string UpperName(MetalType metal)
    {
        return metal.ToString().ToUpperInvariant();
    }
}
