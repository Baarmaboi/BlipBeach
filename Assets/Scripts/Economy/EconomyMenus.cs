/// <summary>
/// Shared open-state for treasure inventory / market panels.
/// </summary>
public static class EconomyMenus
{
    public static bool IsAnyOpen => InventoryUI.IsOpen || MarketUI.IsOpen;
}
