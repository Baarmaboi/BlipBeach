using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player cash and dug treasures. Survives day changes (same as quest progress).
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private KeyCode debugGrantAllMetalsKey = KeyCode.F8;
    [SerializeField] private int debugGrantCountPerMetal = 5;

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

    private void Update()
    {
        if (Input.GetKeyDown(debugGrantAllMetalsKey))
        {
            DebugGrantItemsOfEveryMetal(debugGrantCountPerMetal);
        }
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

    /// <summary>Debug: add several treasures for each metal type from the catalog.</summary>
    public void DebugGrantItemsOfEveryMetal(int countPerMetal = 5)
    {
        if (countPerMetal <= 0)
        {
            return;
        }

        TreasureCatalog catalog = TreasureCatalog.Load();
        if (catalog == null || !catalog.HasAny())
        {
            Debug.LogWarning("Inventory debug: TreasureCatalog missing.");
            return;
        }

        int added = 0;
        for (int m = 0; m < MetalTypes.All.Length; m++)
        {
            MetalType metal = MetalTypes.All[m];
            TreasureDefinition sample = catalog.FindFirstOfMetal(metal);
            if (sample == null)
            {
                Debug.LogWarning($"Inventory debug: no catalog entry for {metal}.");
                continue;
            }

            for (int i = 0; i < countPerMetal; i++)
            {
                treasures.Add(sample);
                added++;
            }
        }

        if (added <= 0)
        {
            return;
        }

        OnChanged?.Invoke();
        Debug.Log($"Inventory debug: added {added} treasures ({countPerMetal} per metal). Count = {treasures.Count}");
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

    /// <summary>Sell one treasure by inventory index. Returns cash earned, or 0 if invalid.</summary>
    public int SellAt(int index, MarketManager market)
    {
        if (market == null || index < 0 || index >= treasures.Count)
        {
            return 0;
        }

        TreasureDefinition treasure = treasures[index];
        int earned = market.GetSellPrice(treasure);
        treasures.RemoveAt(index);
        Cash += earned;
        OnChanged?.Invoke();
        Debug.Log($"Inventory: sold {treasure.FullDisplayName} for ${earned}. Cash = ${Cash}");
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
