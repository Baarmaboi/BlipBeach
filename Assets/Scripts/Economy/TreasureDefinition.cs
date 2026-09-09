using UnityEngine;

/// <summary>
/// Sellable treasure (not a Lost Item). Name + metal type, e.g. GOLD Ring.
/// </summary>
[CreateAssetMenu(fileName = "Treasure_", menuName = "BongoBeach/Treasure Definition")]
public class TreasureDefinition : ScriptableObject
{
    public string treasureId;
    public string displayName;
    public MetalType metalType;
    [Tooltip("Sell price at 100% market. Precious metals ignore daily fluctuation.")]
    public int baseValue = 10;
    [Min(0.1f)]
    public float spawnWeight = 1f;

    [Header("Depth spawn weights (1 = 0-15cm, 2 = 15-30cm, 3 = 30-50cm)")]
    [Tooltip("If all three are 0, metal-type defaults are used (cans shallow, gold deep).")]
    public float weightDepth1;
    public float weightDepth2;
    public float weightDepth3;

    public bool IsPrecious => MetalTypes.IsPrecious(metalType);

    public DepthLevel RollDepth()
    {
        ResolveDepthWeights(out float w1, out float w2, out float w3);
        return DepthLevels.RollWeighted(w1, w2, w3);
    }

    public void ResolveDepthWeights(out float depth1, out float depth2, out float depth3)
    {
        if (weightDepth1 <= 0f && weightDepth2 <= 0f && weightDepth3 <= 0f)
        {
            DepthLevels.DefaultWeights(metalType, out depth1, out depth2, out depth3);
            return;
        }

        depth1 = Mathf.Max(0f, weightDepth1);
        depth2 = Mathf.Max(0f, weightDepth2);
        depth3 = Mathf.Max(0f, weightDepth3);
    }

    public string FullDisplayName
    {
        get
        {
            string metal = MetalTypes.UpperName(metalType);
            if (string.IsNullOrEmpty(displayName))
            {
                return metal;
            }

            return $"{metal} {displayName}";
        }
    }
}
