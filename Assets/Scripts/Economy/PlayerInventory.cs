using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player cash and dug treasures. Survives day changes (same as quest progress).
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    private readonly List<TreasureDefinition> treasures = new List<TreasureDefinition>();

    public int Cash { get; private set; }
    public IReadOnlyList<TreasureDefinition> Treasures => treasures;

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Add(TreasureDefinition treasure)
    {
        if (treasure == null)
        {
            return;
        }

        treasures.Add(treasure);
        OnChanged?.Invoke();
        Debug.Log($"Inventory: added {treasure.FullDisplayName}. Count = {treasures.Count}");
    }

    public int SellAll(MarketManager market)
    {
        if (market == null || treasures.Count == 0)
        {
            return 0;
        }

        int earned = 0;
        for (int i = 0; i < treasures.Count; i++)
        {
            earned += market.GetSellPrice(treasures[i]);
        }

        treasures.Clear();
        Cash += earned;
        OnChanged?.Invoke();
        Debug.Log($"Inventory: sold treasures for ${earned}. Cash = ${Cash}");
        return earned;
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || Cash < amount)
        {
            return false;
        }

        Cash -= amount;
        OnChanged?.Invoke();
        return true;
    }

    public int PreviewSellAll(MarketManager market)
    {
        if (market == null)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < treasures.Count; i++)
        {
            total += market.GetSellPrice(treasures[i]);
        }

        return total;
    }
}
