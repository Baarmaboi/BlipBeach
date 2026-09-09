using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Weighted pool of treasures. Loaded from Resources/Economy, or built-in defaults.
/// </summary>
[CreateAssetMenu(fileName = "TreasureCatalog", menuName = "BongoBeach/Treasure Catalog")]
public class TreasureCatalog : ScriptableObject
{
    public TreasureDefinition[] treasures;

    private static TreasureCatalog cached;

    public static TreasureCatalog Load()
    {
        if (cached != null)
        {
            return cached;
        }

        TreasureCatalog fromResources = Resources.Load<TreasureCatalog>("Economy/TreasureCatalog");
        if (fromResources != null && fromResources.HasAny())
        {
            cached = fromResources;
            return cached;
        }

        TreasureDefinition[] loaded = Resources.LoadAll<TreasureDefinition>("Economy");
        if (loaded != null && loaded.Length > 0)
        {
            cached = CreateInstance<TreasureCatalog>();
            cached.treasures = loaded;
            return cached;
        }

        cached = CreateBuiltIn();
        return cached;
    }

    public bool HasAny()
    {
        if (treasures == null || treasures.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < treasures.Length; i++)
        {
            if (treasures[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    public TreasureDefinition PickForArea(IList<MetalType> favoredMetals, float preciousChance)
    {
        if (!HasAny())
        {
            return null;
        }

        if (Random.value < Mathf.Clamp01(preciousChance))
        {
            TreasureDefinition precious = PickWeighted(IsPrecious);
            if (precious != null)
            {
                return precious;
            }
        }

        TreasureDefinition common = PickWeighted(def => MatchesFavored(def, favoredMetals));
        if (common != null)
        {
            return common;
        }

        return PickWeighted(def => def != null && !def.IsPrecious) ?? PickWeighted(def => def != null);
    }

    private TreasureDefinition PickWeighted(System.Func<TreasureDefinition, bool> predicate)
    {
        float total = 0f;
        for (int i = 0; i < treasures.Length; i++)
        {
            TreasureDefinition def = treasures[i];
            if (def != null && predicate(def))
            {
                total += Mathf.Max(0.01f, def.spawnWeight);
            }
        }

        if (total <= 0f)
        {
            return null;
        }

        float roll = Random.Range(0f, total);
        float running = 0f;
        for (int i = 0; i < treasures.Length; i++)
        {
            TreasureDefinition def = treasures[i];
            if (def == null || !predicate(def))
            {
                continue;
            }

            running += Mathf.Max(0.01f, def.spawnWeight);
            if (roll <= running)
            {
                return def;
            }
        }

        return null;
    }

    private static bool IsPrecious(TreasureDefinition def)
    {
        return def != null && def.IsPrecious;
    }

    private static bool MatchesFavored(TreasureDefinition def, IList<MetalType> favoredMetals)
    {
        if (def == null || def.IsPrecious)
        {
            return false;
        }

        if (favoredMetals == null || favoredMetals.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < favoredMetals.Count; i++)
        {
            if (def.metalType == favoredMetals[i])
            {
                return true;
            }
        }

        return false;
    }

    private static TreasureCatalog CreateBuiltIn()
    {
        TreasureCatalog catalog = CreateInstance<TreasureCatalog>();
        catalog.treasures = new[]
        {
            Make("iron_nail", "Nail", MetalType.Iron, 5, 12f),
            Make("iron_bottle_cap", "Bottle Cap", MetalType.Iron, 4, 10f),
            Make("iron_screw", "Screw", MetalType.Iron, 5, 8f),
            Make("copper_coin", "Coin", MetalType.Copper, 12, 10f),
            Make("copper_wire", "Wire", MetalType.Copper, 10, 8f),
            Make("brass_button", "Button", MetalType.Brass, 14, 10f),
            Make("brass_buckle", "Buckle", MetalType.Brass, 18, 8f),
            Make("brass_fitting", "Fitting", MetalType.Brass, 16, 7f),
            Make("aluminum_can", "Can", MetalType.Aluminum, 7, 12f),
            Make("aluminum_pull_tab", "Pull Tab", MetalType.Aluminum, 6, 10f),
            Make("lead_sinker", "Sinker", MetalType.Lead, 11, 8f),
            Make("lead_weight", "Weight", MetalType.Lead, 12, 6f),
            Make("silver_charm", "Charm", MetalType.Silver, 80, 2f),
            Make("silver_coin", "Coin", MetalType.Silver, 90, 1.5f),
            Make("gold_ring", "Ring", MetalType.Gold, 160, 1f),
            Make("gold_earring", "Earring", MetalType.Gold, 140, 1f)
        };
        return catalog;
    }

    private static TreasureDefinition Make(string id, string name, MetalType metal, int value, float weight)
    {
        TreasureDefinition def = CreateInstance<TreasureDefinition>();
        def.treasureId = id;
        def.displayName = name;
        def.metalType = metal;
        def.baseValue = value;
        def.spawnWeight = weight;
        def.name = $"Treasure_{MetalTypes.UpperName(metal)}_{name.Replace(" ", "")}";
        return def;
    }
}
